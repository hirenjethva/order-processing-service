using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Application.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(string id);
}
