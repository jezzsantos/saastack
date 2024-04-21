using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Memberships;

public class ChangeDefaultOrganizationRequestValidator : AbstractValidator<ChangeDefaultOrganizationRequest>
{
    public ChangeDefaultOrganizationRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.OrganizationId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}