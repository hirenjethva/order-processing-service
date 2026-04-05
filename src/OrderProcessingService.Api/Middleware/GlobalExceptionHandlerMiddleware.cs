using Microsoft.AspNetCore.Mvc;
using OrderProcessingService.Application.Exceptions;

namespace OrderProcessingService.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment)
    {
        _next = next;
        _problemDetailsService = problemDetailsService;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            var problem = CreateProblemDetails(context, ex);
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

            await _problemDetailsService
                .WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem })
                .ConfigureAwait(false);
        }
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception ex)
    {
        var instance = context.Request.Path.HasValue ? context.Request.Path.Value : null;

        switch (ex)
        {
            case ProductNotFoundException e:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Product not found",
                    Detail = string.IsNullOrEmpty(e.Message)
                        ? "The requested product was not found."
                        : e.Message,
                    Instance = instance
                };

            case OrderNotFoundException e:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Order not found",
                    Detail = string.IsNullOrEmpty(e.Message)
                        ? "The requested order was not found."
                        : e.Message,
                    Instance = instance
                };

            case InsufficientStockException e:
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Insufficient stock",
                    Detail = string.IsNullOrEmpty(e.Message)
                        ? $"Insufficient stock for product '{e.ProductId}'."
                        : e.Message,
                    Instance = instance
                };
                problem.Extensions["productId"] = e.ProductId;
                problem.Extensions["requestedQuantity"] = e.RequestedQuantity;
                return problem;
            }

            case InvalidStatusTransitionException e:
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Invalid order status transition",
                    Detail = string.IsNullOrEmpty(e.Message)
                        ? $"Cannot transition from {e.CurrentStatus} to {e.RequestedStatus}."
                        : e.Message,
                    Instance = instance
                };
                problem.Extensions["currentStatus"] = e.CurrentStatus.ToString();
                problem.Extensions["requestedStatus"] = e.RequestedStatus.ToString();
                return problem;
            }

            default:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Detail = _environment.IsDevelopment()
                        ? ex.ToString()
                        : "An unexpected error occurred.",
                    Instance = instance
                };
        }
    }
}
