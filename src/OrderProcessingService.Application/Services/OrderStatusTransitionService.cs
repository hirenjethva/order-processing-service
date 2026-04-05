using System.Linq;
using OrderProcessingService.Application.Common;
using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Application.Services;

/// <summary>
/// Forward: Pending→Confirmed→Processing→Shipped→Delivered.
/// Cancel only from Pending or Confirmed. Delivered and Cancelled are terminal.
/// </summary>
public sealed class OrderStatusTransitionService : IOrderStatusTransitionService
{
    private static readonly Dictionary<OrderStatus, IEnumerable<OrderStatus>> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public Result<Unit, Error> ValidateTransition(OrderStatus current, OrderStatus next)
    {
        var allowedTargets = AllowedTransitions.GetValueOrDefault(current, Enumerable.Empty<OrderStatus>());

        return allowedTargets.Contains(next)
            ? Result<Unit, Error>.Success(default)
            : Result<Unit, Error>.Failure(new InvalidOrderStatusTransitionError(current, next));
    }
}
