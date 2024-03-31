using Common;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PasswordCredentials;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PasswordCredentials;

[Trait("Category", "Unit")]
public class RegisterPersonPasswordRequestValidatorSpec
{
    private readonly RegisterPersonPasswordRequest _dto;
    private readonly RegisterPersonPasswordRequestValidator _validator;

    public RegisterPersonPasswordRequestValidatorSpec()
    {
        _validator = new RegisterPersonPasswordRequestValidator();
        _dto = new RegisterPersonPasswordRequest
        {
            FirstName = "afirstname",
            LastName = "alastname",
            EmailAddress = "auser@company.com",
            Password = "1Password!",
            Timezone = Timezones.Default.Id,
            CountryCode = CountryCodes.Default.Alpha3,
            TermsAndConditionsAccepted = true
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenInvitationTokenIsEmpty_ThenSucceeds()
    {
        _dto.InvitationToken = string.Empty;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenInvitationTokenIsInvalid_ThenThrows()
    {
        _dto.InvitationToken = "aninvalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidInvitationToken);
    }

    [Fact]
    public void WhenEmailIsEmpty_ThenThrows()
    {
        _dto.EmailAddress = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidEmail);
    }

    [Fact]
    public void WhenEmailIsNotEmail_ThenThrows()
    {
        _dto.EmailAddress = "notanemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidEmail);
    }

    [Fact]
    public void WhenPasswordIsEmpty_ThenThrows()
    {
        _dto.Password = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidPassword);
    }

    [Fact]
    public void WhenFirstNameIsEmpty_ThenThrows()
    {
        _dto.FirstName = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidFirstName);
    }

    [Fact]
    public void WhenFirstNameIsInvalid_ThenThrows()
    {
        _dto.FirstName = "aninvalidfirstname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidFirstName);
    }

    [Fact]
    public void WhenLastNameIsEmpty_ThenThrows()
    {
        _dto.LastName = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidLastName);
    }

    [Fact]
    public void WhenLastNameIsInvalid_ThenThrows()
    {
        _dto.LastName = "aninvalidlastname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidLastName);
    }

    [Fact]
    public void WhenTimezoneIsMissing_ThenSucceeds()
    {
        _dto.Timezone = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenTimezoneIsInvalid_ThenThrows()
    {
        _dto.Timezone = "aninvalidtimezone^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterAnyRequestValidator_InvalidTimezone);
    }

    [Fact]
    public void WhenCountryCodeIsMissing_ThenSucceeds()
    {
        _dto.CountryCode = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenCountryCodeIsInvalid_ThenThrows()
    {
        _dto.CountryCode = "aninvalidcountrycode^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterAnyRequestValidator_InvalidCountryCode);
    }

    [Fact]
    public void WhenTermsAndConditionsAcceptedIsFalse_ThenThrows()
    {
        _dto.TermsAndConditionsAccepted = false;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterPersonPasswordRequestValidator_InvalidTermsAndConditionsAccepted);
    }
}