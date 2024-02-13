using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.FeatureFlags;

public class GetFeatureFlagRequestValidator : AbstractValidator<GetFeatureFlagRequest>
{
    public GetFeatureFlagRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .WithMessage(Resources.GetFeatureFlagRequestValidator_InvalidName);
        RuleFor(req => req.TenantId)
            .IsEntityId(idFactory)
            .When(req => req.TenantId.HasValue())
            .WithMessage(Resources.GetFeatureFlagRequestValidator_InvalidTenantId);
        RuleFor(req => req.UserId)
            .IsEntityId(idFactory)
            .WithMessage(Resources.GetFeatureFlagRequestValidator_InvalidUserId);
    }
}