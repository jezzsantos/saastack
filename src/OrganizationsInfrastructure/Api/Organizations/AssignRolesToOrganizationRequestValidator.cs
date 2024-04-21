using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Organizations;

public class AssignRolesToOrganizationRequestValidator : AbstractValidator<AssignRolesToOrganizationRequest>
{
    public AssignRolesToOrganizationRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Roles)
            .NotEmpty()
            .WithMessage(Resources.AssignRolesToOrganizationRequestValidator_InvalidRoles);
        RuleFor(req => req.Roles)
            .ForEach(req => req.Matches(Validations.Role)
                .WithMessage(Resources.AssignRolesToOrganizationRequestValidator_InvalidRole));
    }
}