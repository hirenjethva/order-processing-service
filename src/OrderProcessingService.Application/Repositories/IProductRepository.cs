using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Application.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(string id);

    Task<Product?> ReserveStockAsync(string productId, int quantity);

    /// <summary>Restores stock after a failed order (e.g. compensating reservation).</summary>
    Task ReleaseStockAsync(string productId, int quantity);

    Task SeedProductsAsync();
}
