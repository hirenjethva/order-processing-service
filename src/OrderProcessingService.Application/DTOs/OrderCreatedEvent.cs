namespace OrderProcessingService.Application.DTOs;

public class OrderCreatedEvent
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public List<OrderCreatedEventItem> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public DateTime OccurredAt { get; set; }
}
