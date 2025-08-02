using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class SearchAllOAuth2ClientsRequestValidator : AbstractValidator<SearchAllOAuth2ClientsRequest>
{
    public SearchAllOAuth2ClientsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}