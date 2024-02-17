using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Common.UnitTests;

[Trait("Category", "Unit")]
public class CallerContextExtensionsSpec
{
    [Fact]
    public void WhenToCallAndContextIsNull_ThenReturnsUnknownContext()
    {
        var result = ((ICallerContext?)null).ToCall();

        result.CallId.Should().Be(CallConstants.UncorrelatedCallId);
        result.CallerId.Should().Be(CallConstants.UnknownCallerId);
        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void WhenToCall_ThenReturnsCustomContext()
    {
        var context = new Mock<ICallerContext>();
        context.Setup(c => c.CallId).Returns("acallid");
        context.Setup(c => c.CallerId).Returns("acallerid");
        context.Setup(c => c.TenantId).Returns((string?)null);

        var result = context.Object.ToCall();

        result.CallId.Should().Be("acallid");
        result.CallerId.Should().Be("acallerid");
        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void WhenToCaller_ThenReturnsCallerId()
    {
        var context = new Mock<ICallerContext>();
        context.Setup(c => c.CallerId).Returns("acallerid");

        var result = context.Object.ToCallerId();

        result.Should().Be("acallerid".ToId());
    }
}