using Microsoft.AspNetCore.Mvc;
using OrderProcessingService.Application.Common;

namespace OrderProcessingService.Api.Http;

internal static class OrderErrorResponses
{
    public static IActionResult From(Error error) => error switch
    {
        ValidationError e => new BadRequestObjectResult(new { message = e.Message }),
        ProductNotFoundError e => new NotFoundObjectResult(new
        {
            message = $"Product '{e.ProductId}' was not found.",
            productId = e.ProductId
        }),
        InsufficientStockError e => new ConflictObjectResult(new
        {
            message = $"Insufficient stock for product '{e.ProductId}'.",
            productId = e.ProductId,
            requestedQuantity = e.RequestedQuantity
        }),
        OrderNotFoundError e => new NotFoundObjectResult(new
        {
            message = $"Order '{e.OrderId}' was not found.",
            orderId = e.OrderId
        }),
        InvalidOrderStatusTransitionError e => new ConflictObjectResult(new
        {
            message = $"Cannot transition from {e.CurrentStatus} to {e.RequestedStatus}.",
            currentStatus = e.CurrentStatus,
            requestedStatus = e.RequestedStatus
        }),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
    };
}
