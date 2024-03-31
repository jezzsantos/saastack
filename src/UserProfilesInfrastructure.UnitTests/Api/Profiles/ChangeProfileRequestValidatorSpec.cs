using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using UnitTesting.Common.Validation;
using UserProfilesInfrastructure.Api.Profiles;
using Xunit;

namespace UserProfilesInfrastructure.UnitTests.Api.Profiles;

[Trait("Category", "Unit")]
public class ChangeProfileRequestValidatorSpec
{
    private readonly ChangeProfileRequest _dto;
    private readonly ChangeProfileRequestValidator _validator;

    public ChangeProfileRequestValidatorSpec()
    {
        _validator = new ChangeProfileRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChangeProfileRequest
        {
            UserId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFirstNameIsInvalid_ThenThrows()
    {
        _dto.FirstName = "^aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidFirstName);
    }

    [Fact]
    public void WhenLastNameIsInvalid_ThenThrows()
    {
        _dto.LastName = "^aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidLastName);
    }

    [Fact]
    public void WhenDisplayNameIsInvalid_ThenThrows()
    {
        _dto.DisplayName = "^aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidDisplayName);
    }

    [Fact]
    public void WhenPhoneNumberIsInvalid_ThenThrows()
    {
        _dto.PhoneNumber = "aninvalidnumber";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidPhoneNumber);
    }

    [Fact]
    public void WhenTimezoneIsInvalid_ThenThrows()
    {
        _dto.Timezone = "aninvalidtimezone";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidTimezone);
    }
}