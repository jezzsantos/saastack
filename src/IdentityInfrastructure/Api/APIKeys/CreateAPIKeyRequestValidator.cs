#if TESTINGONLY

using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.APIKeys;

public class CreateAPIKeyRequestValidator : AbstractValidator<CreateAPIKeyForCallerRequest>
{
    public CreateAPIKeyRequestValidator()
    {
        RuleFor(req => req.ExpiresOnUtc)
            .InclusiveBetween(DateTime.UtcNow.Add(Validations.ApiKey.MinimumExpiryPeriod),
                DateTime.UtcNow.Add(Validations.ApiKey.MaximumExpiryPeriod))
            .When(req => req.ExpiresOnUtc.HasValue)
            .WithMessage(Resources.CreateAPIKeyRequestValidator_InvalidExpiresOn.Format(
                Validations.ApiKey.MinimumExpiryPeriod.TotalHours, Validations.ApiKey.MaximumExpiryPeriod.TotalHours));
    }
}
#endif