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
public class DeleteOAuth2ClientRequestValidatorSpec
{
    private readonly DeleteOAuth2ClientRequest _request;
    private readonly DeleteOAuth2ClientRequestValidator _validator;

    public DeleteOAuth2ClientRequestValidatorSpec()
    {
        _validator = new DeleteOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new DeleteOAuth2ClientRequest
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