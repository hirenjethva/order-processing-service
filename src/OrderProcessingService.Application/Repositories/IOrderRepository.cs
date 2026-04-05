using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Application.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id);

    Task<Order> CreateAsync(Order order);

    Task<Order> UpdateAsync(Order order);
}
