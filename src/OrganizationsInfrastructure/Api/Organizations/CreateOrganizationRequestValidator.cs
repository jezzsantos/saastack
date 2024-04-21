using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Organizations;

public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .Matches(Validations.DisplayName)
            .WithMessage(Resources.CreateOrganizationRequestValidator_InvalidName);
    }
}