using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using JetBrains.Annotations;

namespace AncillaryInfrastructure.Api.Usages;

[UsedImplicitly]
public class DeliverUsageRequestValidator : AbstractValidator<DeliverUsageRequest>
{
    public DeliverUsageRequestValidator()
    {
        RuleFor(req => req.Message)
            .NotEmpty()
            .WithMessage(Resources.AnyMessageValidator_InvalidMessage);
    }
}