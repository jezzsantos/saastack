using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class GetOAuth2ClientRequestValidatorSpec
{
    private readonly GetOAuth2ClientRequest _request;
    private readonly GetOAuth2ClientRequestValidator _validator;

    public GetOAuth2ClientRequestValidatorSpec()
    {
        _validator = new GetOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new GetOAuth2ClientRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _request.Id = "aninvalidid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(CommonValidationResources.AnyValidator_InvalidId);
    }
}