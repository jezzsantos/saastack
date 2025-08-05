using Domain.Common.Identity;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class GetOAuth2ConsentRequestValidatorSpec
{
    private readonly GetOAuth2ClientConsentForCallerRequest _dto;
    private readonly GetOAuth2ConsentRequestValidator _validator;

    public GetOAuth2ConsentRequestValidatorSpec()
    {
        _validator = new GetOAuth2ConsentRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new GetOAuth2ClientConsentForCallerRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}