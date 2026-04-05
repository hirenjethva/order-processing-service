using FluentAssertions;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Domain.Enums;

namespace OrderProcessingService.Tests.Services;

public sealed class OrderStatusTransitionServiceTests
{
    private readonly OrderStatusTransitionService _sut = new();

    [Fact]
    public void ValidateTransition_PendingToConfirmed_ReturnsSuccess()
    {
        var currentStatus = OrderStatus.Pending;
        var newStatus = OrderStatus.Confirmed;

        var result = _sut.ValidateTransition(currentStatus, newStatus);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    public void ValidateTransition_AllValidForwardTransitions_ReturnSuccess(
        OrderStatus current,
        OrderStatus next)
    {
        var result = _sut.ValidateTransition(current, next);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Processing, OrderStatus.Delivered)]
    public void ValidateTransition_SkippingState_ReturnsFalse(OrderStatus current, OrderStatus next)
    {
        var result = _sut.ValidateTransition(current, next);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    public void ValidateTransition_FromTerminalState_ReturnsFalse(OrderStatus current, OrderStatus next)
    {
        var result = _sut.ValidateTransition(current, next);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    public void ValidateTransition_CancelFromInvalidState_ReturnsFalse(
        OrderStatus current,
        OrderStatus next)
    {
        var result = _sut.ValidateTransition(current, next);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Processing)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    public void ValidateTransition_BackwardTransition_ReturnsFalse(OrderStatus current, OrderStatus next)
    {
        var result = _sut.ValidateTransition(current, next);

        result.IsSuccess.Should().BeFalse();
    }
}
