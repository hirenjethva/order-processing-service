using OrderProcessingService.Application.Common;
using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Services;

public interface IOrderStatusTransitionService
{
    /// <summary>
    /// Returns success when <paramref name="next"/> is allowed from <paramref name="current"/>;
    /// otherwise returns failure with <see cref="InvalidOrderStatusTransitionError"/> (never throws).
    /// </summary>
    Result<Unit, Error> ValidateTransition(OrderStatus current, OrderStatus next);
}
