using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.APIKeys;

public class CreateAPIKeyRequestValidator : AbstractValidator<CreateAPIKeyRequest>
{
    public CreateAPIKeyRequestValidator()
    {
        RuleFor(req => req.ExpiresOnUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Add(Validations.ApiKey.MinimumExpiryPeriod))
            .When(req => req.ExpiresOnUtc.HasValue)
            .WithMessage(Resources.CreateAPIKeyRequestValidator_InvalidExpiresOn.Format(
                Validations.ApiKey.MinimumExpiryPeriod.TotalHours));
    }
}