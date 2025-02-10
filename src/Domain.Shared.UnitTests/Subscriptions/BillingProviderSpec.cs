using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class BillingProviderSpec
{
    private readonly BillingProvider _provider;

    public BillingProviderSpec()
    {
        _provider = BillingProvider.Create("aprovidername",
            new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }).Value;
    }

    [Fact]
    public void WhenCreate_ThenReturns()
    {
        var state = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        };

        var result = BillingProvider.Create("aprovidername", state).Value;

        result.State.Should().OnlyContain(x => x.Key == "aname" && x.Value == "avalue");
        result.Name.Should().Be("aprovidername");
        result.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithEmptyProviderName_ThenReturnsError()
    {
        var result = BillingProvider.Create(string.Empty, new SubscriptionMetadata());

        result.Should().BeError(ErrorCode.Validation, Resources.BillingProvider_InvalidName);
    }

    [Fact]
    public void WhenCreateWithInvalidProviderName_ThenReturnsError()
    {
        var result = BillingProvider.Create("^^aninvalidname^^", new SubscriptionMetadata());

        result.Should().BeError(ErrorCode.Validation, Resources.BillingProvider_InvalidName);
    }

    [Fact]
    public void WhenCreateWithEmptyState_ThenReturnsError()
    {
        var result = BillingProvider.Create("aprovidername", new SubscriptionMetadata());

        result.Should().BeError(ErrorCode.Validation, Resources.BillingProvider_InvalidMetadata);
    }

    [Fact]
    public void WhenCreateWithProviderAndState_ThenInitialized()
    {
        var state = new SubscriptionMetadata
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        };

        var result = BillingProvider.Create("aprovidername", state).Value;

        result.State.Count.Should().Be(3);
        result.State.Should().BeSameAs(state);
        result.Name.Should().Be("aprovidername");
        result.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void WhenIsCurrentProviderAndDifferentProviderName_ThenReturnsFalse()
    {
        var result = _provider.IsCurrentProvider("anotherprovidername");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsCurrentProviderAndSameProviderName_ThenReturnsTrue()
    {
        var result = _provider.IsCurrentProvider("aprovidername");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenChangeState_ThenReturnsChangedState()
    {
        var state = new SubscriptionMetadata
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        };

        var result = _provider.ChangeState(state);

        result.State.Count.Should().Be(3);
        result.State.Should().Contain(x => x.Key == "aname1" && x.Value == "avalue1");
        result.State.Should().Contain(x => x.Key == "aname2" && x.Value == "avalue2");
        result.State.Should().Contain(x => x.Key == "aname3" && x.Value == "avalue3");
    }
}