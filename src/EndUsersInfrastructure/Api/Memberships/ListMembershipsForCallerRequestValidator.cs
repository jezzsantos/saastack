using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Memberships;

public class ListMembershipsForCallerRequestValidator : AbstractValidator<ListMembershipsForCallerRequest>
{
    public ListMembershipsForCallerRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}