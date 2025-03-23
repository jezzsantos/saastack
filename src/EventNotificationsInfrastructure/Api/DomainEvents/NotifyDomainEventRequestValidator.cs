using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using JetBrains.Annotations;

namespace EventNotificationsInfrastructure.Api.DomainEvents;

[UsedImplicitly]
public class NotifyDomainEventRequestValidator : AbstractValidator<NotifyDomainEventRequest>
{
    public NotifyDomainEventRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.NotifyDomainEventRequestValidator_InvalidMessage);
        RuleFor(req => req.SubscriptionName)
            .NotEmpty()
            .WithMessage(Resources.NotifyDomainEventRequestValidator_InvalidSubscriptionName);
    }
}