using Microsoft.AspNetCore.Mvc;
using OrderProcessingService.Api.Http;
using OrderProcessingService.Application.Common;
using OrderProcessingService.Application.DTOs;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Domain.Entities;

namespace OrderProcessingService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Creates a new order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(request).ConfigureAwait(false);
        if (!result.IsSuccess)
            return OrderErrorResponses.From(result.Error!);

        var order = result.Value!;
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>Gets an order by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder([FromRoute] string id)
    {
        var result = await _orderService.GetOrderAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
            return OrderErrorResponses.From(result.Error!);

        return Ok(result.Value);
    }

    /// <summary>Updates the status of an order.</summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute] string id, [FromBody] UpdateOrderStatusRequest body)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, body.Status).ConfigureAwait(false);
        if (!result.IsSuccess)
            return OrderErrorResponses.From(result.Error!);

        return Ok(result.Value);
    }
}
