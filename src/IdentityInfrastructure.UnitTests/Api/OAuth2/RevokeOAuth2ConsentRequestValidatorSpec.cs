using Domain.Common.Identity;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class RevokeOAuth2ConsentRequestValidatorSpec
{
    private readonly RevokeOAuth2ClientConsentForCallerRequest _request;
    private readonly RevokeOAuth2ConsentRequestValidator _validator;

    public RevokeOAuth2ConsentRequestValidatorSpec()
    {
        _validator = new RevokeOAuth2ConsentRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new RevokeOAuth2ClientConsentForCallerRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }
}