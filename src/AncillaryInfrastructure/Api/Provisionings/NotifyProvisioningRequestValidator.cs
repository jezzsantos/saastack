using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Provisionings;

[UsedImplicitly]
public class NotifyProvisioningRequestValidator : AbstractValidator<NotifyProvisioningRequest>
{
    public NotifyProvisioningRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.AnyQueueMessageValidator_InvalidMessage);
    }
}