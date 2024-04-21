using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Organizations;

public class ChangeOrganizationRequestValidator : AbstractValidator<ChangeOrganizationRequest>
{
    public ChangeOrganizationRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Name)
            .Matches(Validations.DisplayName)
            .When(req => req.Name.Exists())
            .WithMessage(Resources.ChangeOrganizationRequestValidator_InvalidName);
    }
}