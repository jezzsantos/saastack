using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Provisionings;

[UsedImplicitly]
public class DeliverProvisioningRequestValidator : AbstractValidator<DeliverProvisioningRequest>
{
    public DeliverProvisioningRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.AnyQueueMessageValidator_InvalidMessage);
    }
}