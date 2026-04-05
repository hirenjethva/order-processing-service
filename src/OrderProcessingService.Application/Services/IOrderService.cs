using OrderProcessingService.Application.Common;
using OrderProcessingService.Application.DTOs;
using OrderProcessingService.Domain.Entities;
using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Services;

public interface IOrderService
{
    Task<Result<Order, Error>> CreateOrderAsync(CreateOrderRequest request);

    Task<Result<Order, Error>> GetOrderAsync(string id);

    Task<Result<Order, Error>> UpdateOrderStatusAsync(string id, OrderStatus newStatus);
}
