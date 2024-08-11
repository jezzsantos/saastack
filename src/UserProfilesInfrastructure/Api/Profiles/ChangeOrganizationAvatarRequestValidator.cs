using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;

namespace UserProfilesInfrastructure.Api.Profiles;

public class ChangeProfileAvatarRequestValidator : AbstractValidator<ChangeProfileAvatarRequest>
{
    public ChangeProfileAvatarRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}