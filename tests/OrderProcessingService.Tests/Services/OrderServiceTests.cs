using FluentAssertions;
using Moq;
using OrderProcessingService.Application.Caching;
using OrderProcessingService.Application.Common;
using OrderProcessingService.Application.DTOs;
using OrderProcessingService.Application.Messaging;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Domain.Entities;
using OrderProcessingService.Domain.Enums;
using OrderProcessingService.Domain.ValueObjects;

namespace OrderProcessingService.Tests.Services;

public sealed class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepository.Object,
            _productRepository.Object,
            _cacheService.Object,
            _messagePublisher.Object,
            new OrderStatusTransitionService());
    }

    private static Product CreateProduct(string id, string name, decimal price, int stock) =>
        new()
        {
            Id = id,
            Name = name,
            Description = "Test product",
            Price = price,
            StockQuantity = stock,
            CreatedAt = DateTime.UtcNow
        };

    private static Order CreateOrder(string id, OrderStatus status) =>
        new()
        {
            Id = id,
            CustomerId = "customer-1",
            Items = new List<OrderItem>(),
            TotalAmount = 100m,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task CreateOrderAsync_WithValidItems_CreatesOrderSuccessfully()
    {
        var laptop = CreateProduct("prod-1", "Laptop", 999.99m, 10);
        var afterReserve = CreateProduct("prod-1", "Laptop", 999.99m, 8);

        _productRepository
            .Setup(p => p.GetByIdAsync("prod-1"))
            .ReturnsAsync(laptop);
        _productRepository
            .Setup(p => p.ReserveStockAsync("prod-1", 2))
            .ReturnsAsync(afterReserve);
        _orderRepository
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) =>
            {
                o.Id = "order-1";
                return o;
            });

        var result = await _sut.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "customer-1",
            Items = new List<OrderItemRequest> { new() { ProductId = "prod-1", Quantity = 2 } }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CustomerId.Should().Be("customer-1");
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalAmount.Should().Be(1999.98m);
        result.Value.Status.Should().Be(OrderStatus.Pending);

        _messagePublisher.Verify(
            x => x.PublishAsync("order.created", It.IsAny<OrderCreatedEvent>()),
            Times.Once);
        _orderRepository.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInsufficientStock_FailsWithInsufficientStockError()
    {
        var lowStock = CreateProduct("prod-1", "Laptop", 999.99m, 1);

        _productRepository.Setup(p => p.GetByIdAsync("prod-1")).ReturnsAsync(lowStock);
        _productRepository.Setup(p => p.ReserveStockAsync("prod-1", 5)).ReturnsAsync((Product?)null);

        var result = await _sut.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "customer-1",
            Items = new List<OrderItemRequest> { new() { ProductId = "prod-1", Quantity = 5 } }
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<InsufficientStockError>();

        _orderRepository.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<OrderCreatedEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentProduct_FailsWithProductNotFoundError()
    {
        _productRepository.Setup(p => p.GetByIdAsync("non-existent")).ReturnsAsync((Product?)null);

        var result = await _sut.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "customer-1",
            Items = new List<OrderItemRequest> { new() { ProductId = "non-existent", Quantity = 1 } }
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ProductNotFoundError>();

        _orderRepository.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidItems_InvalidatesCacheAfterCreation()
    {
        var product = CreateProduct("prod-1", "Laptop", 999.99m, 10);

        _productRepository.Setup(p => p.GetByIdAsync("prod-1")).ReturnsAsync(product);
        _productRepository.Setup(p => p.ReserveStockAsync("prod-1", 1)).ReturnsAsync(product);
        _orderRepository
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) =>
            {
                o.Id = "order-1";
                return o;
            });

        await _sut.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = "customer-1",
            Items = new List<OrderItemRequest> { new() { ProductId = "prod-1", Quantity = 1 } }
        });

        _cacheService.Verify(x => x.DeleteAsync(ProductCacheKeys.Product("prod-1")), Times.Once);
        _cacheService.Verify(x => x.DeleteByPatternAsync(ProductCacheKeys.ProductsPattern), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_WithExistingId_ReturnsOrder()
    {
        var order = CreateOrder("order-1", OrderStatus.Pending);
        _orderRepository.Setup(r => r.GetByIdAsync("order-1")).ReturnsAsync(order);

        var result = await _sut.GetOrderAsync("order-1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("order-1");
        result.Value.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task GetOrderAsync_WithNonExistentId_FailsWithOrderNotFoundError()
    {
        _orderRepository.Setup(r => r.GetByIdAsync("bad-id")).ReturnsAsync((Order?)null);

        var result = await _sut.GetOrderAsync("bad-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<OrderNotFoundError>();
    }
}
