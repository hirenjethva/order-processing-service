using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Common;

public abstract record Error;

public record ValidationError(string Message) : Error;

public record ProductNotFoundError(string ProductId) : Error;

public record InsufficientStockError(string ProductId, int RequestedQuantity) : Error;

public record OrderNotFoundError(string OrderId) : Error;

public record InvalidOrderStatusTransitionError(OrderStatus CurrentStatus, OrderStatus RequestedStatus) : Error;
