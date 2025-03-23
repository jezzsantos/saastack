using EventNotificationsInfrastructure.Api.DomainEvents;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using UnitTesting.Common.Validation;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.Api.DomainEvents;

[Trait("Category", "Unit")]
public class NotifyDomainEventRequestValidatorSpec
{
    private readonly NotifyDomainEventRequest _dto;
    private readonly NotifyDomainEventRequestValidator _validator;

    public NotifyDomainEventRequestValidatorSpec()
    {
        _validator = new NotifyDomainEventRequestValidator();
        _dto = new NotifyDomainEventRequest
        {
            Message = "amessage",
            SubscriptionName = "asubscriber"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenMessageIsNull_ThenThrows()
    {
        _dto.Message = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.NotifyDomainEventRequestValidator_InvalidMessage);
    }

    [Fact]
    public void WhenSubscriberIsNull_ThenThrows()
    {
        _dto.SubscriptionName = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.NotifyDomainEventRequestValidator_InvalidSubscriptionName);
    }
}