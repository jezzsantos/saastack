using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Signings;
using FluentAssertions;
using Moq;
using Xunit;

namespace SigningsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SigningRequestRootSpec
{
    private readonly SigningRequestRoot _request;

    public SigningRequestRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();

        _request = SigningRequestRoot.Create(recorder.Object, identifierFactory.Object, "anorganizationid".ToId())
            .Value;
    }

    [Fact]
    public void WhenCreate_ThenCreates()
    {
        _request.OrganizationId.Should().Be("anorganizationid".ToId());
        _request.Events.Last().Should().BeOfType<Created>();
    }
}