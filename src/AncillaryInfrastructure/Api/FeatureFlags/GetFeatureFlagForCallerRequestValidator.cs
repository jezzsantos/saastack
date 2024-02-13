using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.FeatureFlags;

public class GetFeatureFlagForCallerRequestValidator : AbstractValidator<GetFeatureFlagForCallerRequest>
{
    public GetFeatureFlagForCallerRequestValidator()
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .WithMessage(Resources.GetFeatureFlagRequestValidator_InvalidName);
    }
}