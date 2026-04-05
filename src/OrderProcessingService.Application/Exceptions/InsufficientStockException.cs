namespace OrderProcessingService.Application.Exceptions;

public class InsufficientStockException : Exception
{
    public string ProductId { get; }

    public int RequestedQuantity { get; }

    public InsufficientStockException(string productId, int requestedQuantity)
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
    }

    public InsufficientStockException(string productId, int requestedQuantity, string message)
        : base(message)
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
    }

    public InsufficientStockException(string productId, int requestedQuantity, string message, Exception innerException)
        : base(message, innerException)
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
    }
}
