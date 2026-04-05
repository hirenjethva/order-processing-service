using OrderProcessingService.Application.Caching;
using OrderProcessingService.Application.Common;
using OrderProcessingService.Application.DTOs;
using OrderProcessingService.Application.Messaging;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Domain.Entities;
using OrderProcessingService.Domain.Enums;
using OrderProcessingService.Domain.ValueObjects;

namespace OrderProcessingService.Application.Services;

public sealed class OrderService : IOrderService
{
    private const string OrderCreatedRoutingKey = "order.created";

    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IOrderStatusTransitionService _statusTransitions;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICacheService cacheService,
        IMessagePublisher messagePublisher,
        IOrderStatusTransitionService statusTransitions)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _cacheService = cacheService;
        _messagePublisher = messagePublisher;
        _statusTransitions = statusTransitions;
    }

    public async Task<Result<Order, Error>> CreateOrderAsync(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            return Result<Order, Error>.Failure(new ValidationError("CustomerId is required."));

        if (request.Items.Count == 0)
            return Result<Order, Error>.Failure(new ValidationError("Order must contain at least one line item."));

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0)
                return Result<Order, Error>.Failure(
                    new ValidationError($"Quantity must be greater than zero for product '{line.ProductId}'."));
        }

        foreach (var line in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId).ConfigureAwait(false);
            if (product is null)
                return Result<Order, Error>.Failure(new ProductNotFoundError(line.ProductId));
        }

        var reservations = new List<(string ProductId, int Quantity)>();
        var orderItems = new List<OrderItem>();

        foreach (var line in request.Items)
        {
            var reserved = await _productRepository.ReserveStockAsync(line.ProductId, line.Quantity)
                .ConfigureAwait(false);
            if (reserved is null)
            {
                await CompensateReservationsAsync(reservations).ConfigureAwait(false);
                return Result<Order, Error>.Failure(
                    new InsufficientStockError(line.ProductId, line.Quantity));
            }

            reservations.Add((line.ProductId, line.Quantity));
            orderItems.Add(new OrderItem
            {
                ProductId = reserved.Id,
                ProductName = reserved.Name,
                Quantity = line.Quantity,
                UnitPrice = reserved.Price
            });
        }

        var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = orderItems,
            TotalAmount = total,
            Status = OrderStatus.Pending
        };

        Order created;
        try
        {
            created = await _orderRepository.CreateAsync(order).ConfigureAwait(false);
        }
        catch
        {
            await CompensateReservationsAsync(reservations).ConfigureAwait(false);
            throw;
        }

        var evt = new OrderCreatedEvent
        {
            OrderId = created.Id,
            CustomerId = created.CustomerId,
            Items = created.Items.Select(i => new OrderCreatedEventItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = created.TotalAmount,
            OccurredAt = DateTime.UtcNow
        };

        await _messagePublisher.PublishAsync(OrderCreatedRoutingKey, evt).ConfigureAwait(false);

        await InvalidateProductCachesAsync(reservations.Select(r => r.ProductId)).ConfigureAwait(false);

        return Result<Order, Error>.Success(created);
    }

    public async Task<Result<Order, Error>> GetOrderAsync(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (order is null)
            return Result<Order, Error>.Failure(new OrderNotFoundError(id));

        return Result<Order, Error>.Success(order);
    }

    public async Task<Result<Order, Error>> UpdateOrderStatusAsync(string id, OrderStatus newStatus)
    {
        var order = await _orderRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (order is null)
            return Result<Order, Error>.Failure(new OrderNotFoundError(id));

        var transition = _statusTransitions.ValidateTransition(order.Status, newStatus);
        if (!transition.IsSuccess)
            return Result<Order, Error>.Failure(transition.Error!);

        order.Status = newStatus;
        var updated = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
        return Result<Order, Error>.Success(updated);
    }

    private async Task CompensateReservationsAsync(List<(string ProductId, int Quantity)> reservations)
    {
        foreach (var (productId, quantity) in reservations.AsEnumerable().Reverse())
        {
            await _productRepository.ReleaseStockAsync(productId, quantity).ConfigureAwait(false);
        }
    }

    private async Task InvalidateProductCachesAsync(IEnumerable<string> productIds)
    {
        foreach (var id in productIds.Distinct())
        {
            await _cacheService.DeleteAsync(ProductCacheKeys.Product(id)).ConfigureAwait(false);
        }

        await _cacheService.DeleteByPatternAsync(ProductCacheKeys.ProductsPattern).ConfigureAwait(false);
    }
}
