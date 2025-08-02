using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class SearchAllOAuth2ClientsRequestValidatorSpec
{
    private readonly SearchAllOAuth2ClientsRequest _dto;
    private readonly SearchAllOAuth2ClientsRequestValidator _validator;

    public SearchAllOAuth2ClientsRequestValidatorSpec()
    {
        _validator =
            new SearchAllOAuth2ClientsRequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new SearchAllOAuth2ClientsRequest();
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}