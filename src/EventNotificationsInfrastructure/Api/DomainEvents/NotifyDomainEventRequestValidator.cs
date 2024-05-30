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
            .WithMessage(Resources.AnyQueueMessageValidator_InvalidMessage);
    }
}