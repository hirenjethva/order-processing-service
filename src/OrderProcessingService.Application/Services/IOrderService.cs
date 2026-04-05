using OrderProcessingService.Application.DTOs;
using OrderProcessingService.Domain.Entities;
using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);

    Task<Order?> GetOrderAsync(string id);

    Task<Order> UpdateOrderStatusAsync(string id, OrderStatus newStatus);
}
