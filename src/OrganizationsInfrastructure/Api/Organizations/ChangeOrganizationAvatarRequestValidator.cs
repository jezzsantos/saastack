using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;

namespace OrganizationsInfrastructure.Api.Organizations;

public class ChangeOrganizationAvatarRequestValidator : AbstractValidator<ChangeOrganizationAvatarRequest>
{
    public ChangeOrganizationAvatarRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}