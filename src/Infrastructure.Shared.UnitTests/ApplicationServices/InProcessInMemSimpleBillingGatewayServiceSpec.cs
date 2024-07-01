using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class InProcessInMemSimpleBillingGatewayServiceSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly InProcessInMemSimpleBillingGatewayService _service;

    public InProcessInMemSimpleBillingGatewayServiceSpec()
    {
        _caller = new Mock<ICallerContext>();
        _service = new InProcessInMemSimpleBillingGatewayService();
    }

    [Fact]
    public async Task WhenSubscribeAsync_ThenReturns()
    {
        var buyer = new SubscriptionBuyer
        {
            Address = new ProfileAddress { CountryCode = CountryCodes.NewZealand.ToString() },
            EmailAddress = "auser@company.com",
            Id = "abuyerid",
            Name = new PersonName { FirstName = "afirstname" },
            Subscriber = new Subscriber
            {
                EntityId = "anentityid",
                EntityType = "anentitytype"
            },
            PhoneNumber = "aphonenumber"
        };

        var result = await _service.SubscribeAsync(_caller.Object, buyer, SubscribeOptions.Immediately,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(2);
        result.Value[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.BuyerId].Should().Be("abuyerid");
        result.Value[SinglePlanBillingStateInterpreter.Constants.MetadataProperties.SubscriptionId].Should()
            .NotBeNull();
    }
}