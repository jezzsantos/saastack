using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

namespace WebsiteHost.Api.FeatureFlags;

public class GetFeatureFlagForCallerRequestValidator : AbstractValidator<GetFeatureFlagForCallerRequest>
{
    public GetFeatureFlagForCallerRequestValidator()
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .WithMessage(Resources.GetFeatureFlagForCallerRequestValidator_InvalidName);
    }
}