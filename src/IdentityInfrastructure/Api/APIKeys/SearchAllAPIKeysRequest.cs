using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.APIKeys;

public class SearchAllAPIKeysRequestValidator : AbstractValidator<SearchAllAPIKeysForCallerRequest>
{
    public SearchAllAPIKeysRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}