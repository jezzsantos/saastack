using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using UnitTesting.Common.Validation;
using UserProfilesInfrastructure.Api.Profiles;
using Xunit;

namespace UserProfilesInfrastructure.UnitTests.Api.Profiles;

[Trait("Category", "Unit")]
public class ChangeProfileContactAddressRequestValidatorSpec
{
    private readonly ChangeProfileContactAddressRequest _dto;
    private readonly ChangeProfileContactAddressRequestValidator _validator;

    public ChangeProfileContactAddressRequestValidatorSpec()
    {
        _validator = new ChangeProfileContactAddressRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChangeProfileContactAddressRequest
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
    public void WhenLine1IsInvalid_ThenThrows()
    {
        _dto.Line1 = "^aninvalidline^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine1);
    }

    [Fact]
    public void WhenLine2IsInvalid_ThenThrows()
    {
        _dto.Line2 = "^aninvalidline^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine2);
    }

    [Fact]
    public void WhenLine3IsInvalid_ThenThrows()
    {
        _dto.Line3 = "^aninvalidline^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidLine3);
    }

    [Fact]
    public void WhenCityIsInvalid_ThenThrows()
    {
        _dto.City = "^aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidCity);
    }

    [Fact]
    public void WhenStateIsInvalid_ThenThrows()
    {
        _dto.State = "^aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidState);
    }

    [Fact]
    public void WhenCountryCodeIsInvalid_ThenThrows()
    {
        _dto.CountryCode = "aninvalidcode";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidCountryCode);
    }

    [Fact]
    public void WhenZipIsInvalid_ThenThrows()
    {
        _dto.Zip = "^aninvalidzip^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeProfileContactAddressRequestValidator_InvalidZip);
    }
}