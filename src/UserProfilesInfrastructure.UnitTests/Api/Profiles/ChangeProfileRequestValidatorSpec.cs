using Common;
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
            UserId = "anid",
            FirstName = "afirstname",
            LastName = "alastname",
            DisplayName = "adisplayname",
            PhoneNumber = "+6498876986",
            Locale = Locales.Default.ToString(),
            Timezone = Timezones.Default.ToString()
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFirstNameIsNull_ThenSucceeds()
    {
        _dto.FirstName = null;

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
    public void WhenLastNameIsNull_ThenSucceeds()
    {
        _dto.LastName = null;

        _validator.ValidateAndThrow(_dto);
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
    public void WhenDisplayNameIsNull_ThenSucceeds()
    {
        _dto.DisplayName = null;

        _validator.ValidateAndThrow(_dto);
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
    public void WhenPhoneNumberIsNull_ThenSucceeds()
    {
        _dto.PhoneNumber = null;

        _validator.ValidateAndThrow(_dto);
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
    public void WhenLocaleIsNull_ThenSucceeds()
    {
        _dto.Locale = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenLocaleIsInvalid_ThenThrows()
    {
        _dto.Locale = "aninvalidlocale";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileRequestValidator_InvalidLocale);
    }

    [Fact]
    public void WhenTimezoneIsNull_ThenSucceeds()
    {
        _dto.Timezone = null;

        _validator.ValidateAndThrow(_dto);
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