using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class ListOAuth2ClientsRequestValidator : AbstractValidator<SearchAllOAuth2ClientsRequest>
{
    public ListOAuth2ClientsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}