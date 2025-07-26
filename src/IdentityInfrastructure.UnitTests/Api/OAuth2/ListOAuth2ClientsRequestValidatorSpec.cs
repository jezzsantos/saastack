using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class ListOAuth2ClientsRequestValidatorSpec
{
    private readonly SearchAllOAuth2ClientsRequest _request;
    private readonly ListOAuth2ClientsRequestValidator _validator;

    public ListOAuth2ClientsRequestValidatorSpec()
    {
        _validator = new ListOAuth2ClientsRequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _request = new SearchAllOAuth2ClientsRequest();
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }
}