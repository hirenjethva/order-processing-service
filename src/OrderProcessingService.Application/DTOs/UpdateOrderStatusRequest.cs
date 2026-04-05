using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.DTOs;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
