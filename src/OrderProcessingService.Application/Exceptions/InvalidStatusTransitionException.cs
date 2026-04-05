using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Exceptions;

public class InvalidStatusTransitionException : Exception
{
    public OrderStatus CurrentStatus { get; }

    public OrderStatus RequestedStatus { get; }

    public InvalidStatusTransitionException(OrderStatus currentStatus, OrderStatus requestedStatus)
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public InvalidStatusTransitionException(OrderStatus currentStatus, OrderStatus requestedStatus, string message)
        : base(message)
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public InvalidStatusTransitionException(OrderStatus currentStatus, OrderStatus requestedStatus, string message, Exception innerException)
        : base(message, innerException)
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }
}
