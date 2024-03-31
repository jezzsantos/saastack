using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Api.Profiles;

public class ChangeProfileRequestValidator : AbstractValidator<ChangeProfileRequest>
{
    public ChangeProfileRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.FirstName)
            .Matches(Validations.FirstName)
            .When(req => req.FirstName.HasValue())
            .WithMessage(Resources.ChangeProfileRequestValidator_InvalidFirstName);
        RuleFor(req => req.LastName)
            .Matches(Validations.LastName)
            .When(req => req.LastName.HasValue())
            .WithMessage(Resources.ChangeProfileRequestValidator_InvalidLastName);
        RuleFor(req => req.DisplayName)
            .Matches(Validations.DisplayName)
            .When(req => req.DisplayName.HasValue())
            .WithMessage(Resources.ChangeProfileRequestValidator_InvalidDisplayName);
        RuleFor(req => req.PhoneNumber)
            .Matches(Validations.PhoneNumber)
            .When(req => req.PhoneNumber.HasValue())
            .WithMessage(Resources.ChangeProfileRequestValidator_InvalidPhoneNumber);
        RuleFor(req => req.Timezone)
            .Matches(Validations.Timezone)
            .When(req => req.Timezone.HasValue())
            .WithMessage(Resources.ChangeProfileRequestValidator_InvalidTimezone);
    }
}