namespace OrderProcessingService.Application.DTOs;

public class OrderCreatedEventItem
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
