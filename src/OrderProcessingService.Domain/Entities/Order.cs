using OrderProcessingService.Domain.Enums;
using OrderProcessingService.Domain.ValueObjects;

namespace OrderProcessingService.Domain.Entities;

public class Order
{
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
