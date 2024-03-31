using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using EndUsersDomain;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.EndUsers;

public class UnassignPlatformRolesRequestValidator : AbstractValidator<UnassignPlatformRolesRequest>
{
    public UnassignPlatformRolesRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Roles)
            .NotEmpty()
            .WithMessage(Resources.AssignPlatformRolesRequestValidator_InvalidRoles);
        RuleFor(req => req.Roles)
            .ForEach(req => req.Matches(Validations.Role)
                .WithMessage(Resources.AssignPlatformRolesRequestValidator_InvalidRole));
    }
}