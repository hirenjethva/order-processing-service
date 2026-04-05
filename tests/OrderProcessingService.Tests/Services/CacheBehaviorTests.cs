using FluentAssertions;
using Moq;
using OrderProcessingService.Application.Caching;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Tests.Services;

public sealed class CacheBehaviorTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly ProductCatalogService _sut;

    public CacheBehaviorTests()
    {
        _sut = new ProductCatalogService(_productRepository.Object, _cacheService.Object);
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

    [Fact]
    public async Task GetProducts_WhenCacheHit_ReturnsCachedValueWithoutHittingDatabase()
    {
        var cachedProducts = new List<Product> { CreateProduct("prod-1", "Laptop", 999m, 10) };
        _cacheService
            .Setup(c => c.GetAsync<List<Product>>(ProductCacheKeys.AllProducts))
            .ReturnsAsync(cachedProducts);

        var result = await _sut.GetAllAsync();

        result.Should().BeEquivalentTo(cachedProducts, options => options.WithStrictOrdering());
        _productRepository.Verify(p => p.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetProducts_WhenCacheMiss_FetchesFromDatabaseAndPopulatesCache()
    {
        _cacheService
            .Setup(c => c.GetAsync<List<Product>>(ProductCacheKeys.AllProducts))
            .ReturnsAsync((List<Product>?)null);

        var dbProducts = new List<Product> { CreateProduct("prod-1", "Laptop", 999m, 10) };
        _productRepository.Setup(p => p.GetAllAsync()).ReturnsAsync(dbProducts);

        var result = await _sut.GetAllAsync();

        result.Should().BeEquivalentTo(dbProducts, options => options.WithStrictOrdering());
        _productRepository.Verify(p => p.GetAllAsync(), Times.Once);
        _cacheService.Verify(
            c => c.SetAsync(
                ProductCacheKeys.AllProducts,
                It.IsAny<List<Product>>(),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }
}
