using Application.Resources.Shared;
using FluentAssertions;
using FluentValidation;
using SigningsInfrastructure.Api.Signings;
using UnitTesting.Common.Validation;
using Xunit;

namespace SigningsInfrastructure.UnitTests.Api.Signings;

[Trait("Category", "Unit")]
public class SigneeValidatorSpec
{
    private readonly Signee _dto;
    private readonly SigneeValidator _validator;

    public SigneeValidatorSpec()
    {
        _validator = new SigneeValidator();
        _dto = new Signee
        {
            EmailAddress = "auser@company.com",
            PhoneNumber = "+6498876986"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmailAddressIsInvalid_ThenThrows()
    {
        _dto.EmailAddress = "notanemailaddress";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SigneeValidator_InvalidEmailAddress);
    }

    [Fact]
    public void WhenEmailAddressIsValid_ThenSucceeds()
    {
        _dto.EmailAddress = "auser@company.com";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenPhoneNumberIsInvalid_ThenThrows()
    {
        _dto.PhoneNumber = "notaphonenumber";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SigneeValidator_InvalidPhoneNumber);
    }
}