using OrderProcessingService.Application.Caching;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Application.Services;

public sealed class ProductCatalogService : IProductCatalogService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cache;

    public ProductCatalogService(IProductRepository productRepository, ICacheService cache)
    {
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<Product>>(ProductCacheKeys.AllProducts).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var products = await _productRepository.GetAllAsync().ConfigureAwait(false);
        await _cache.SetAsync(ProductCacheKeys.AllProducts, products, CacheTtl).ConfigureAwait(false);
        return products;
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        var cached = await _cache.GetAsync<Product>(ProductCacheKeys.Product(id)).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (product is null)
            return null;

        await _cache.SetAsync(ProductCacheKeys.Product(id), product, CacheTtl).ConfigureAwait(false);
        return product;
    }
}
