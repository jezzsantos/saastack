using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;

namespace OrganizationsInfrastructure.Api.Organizations;

public class GetOrganizationRequestValidator : AbstractValidator<GetOrganizationRequest>
{
    public GetOrganizationRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(x => x.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}