using Common;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.MachineCredentials;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MachineCredentials;

[Trait("Category", "Unit")]
public class RegisterMachineRequestValidatorSpec
{
    private readonly RegisterMachineRequest _dto;
    private readonly RegisterMachineRequestValidator _validator;

    public RegisterMachineRequestValidatorSpec()
    {
        _validator = new RegisterMachineRequestValidator();
        _dto = new RegisterMachineRequest
        {
            Name = "amachinename",
            Timezone = Timezones.Default.Id,
            CountryCode = CountryCodes.Default.Alpha3
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNameIsInvalid_ThenThrows()
    {
        _dto.Name = "aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterMachineRequestValidator_InvalidName);
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
}