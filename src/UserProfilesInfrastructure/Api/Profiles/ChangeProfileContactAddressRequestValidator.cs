using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using UserProfilesDomain;

namespace UserProfilesInfrastructure.Api.Profiles;

public class ChangeProfileContactAddressRequestValidator : AbstractValidator<ChangeProfileContactAddressRequest>
{
    public ChangeProfileContactAddressRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Line1)
            .Matches(Validations.Address.Line)
            .When(req => req.Line1.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine1);
        RuleFor(req => req.Line2)
            .Matches(Validations.Address.Line)
            .When(req => req.Line2.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine2);
        RuleFor(req => req.Line3)
            .Matches(Validations.Address.Line)
            .When(req => req.Line3.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine3);
        RuleFor(req => req.City)
            .Matches(Validations.Address.City)
            .When(req => req.City.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidCity);
        RuleFor(req => req.State)
            .Matches(Validations.Address.State)
            .When(req => req.State.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidState);
        RuleFor(req => req.CountryCode)
            .Matches(Validations.Address.CountryCode)
            .When(req => req.CountryCode.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidCountryCode);
        RuleFor(req => req.Zip)
            .Matches(Validations.Address.Zip)
            .When(req => req.Zip.HasValue())
            .WithMessage(Resources.ChangeProfileContactAddressRequestValidator_InvalidZip);
    }
}