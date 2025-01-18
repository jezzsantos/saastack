using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Audits;

public class SearchAllAuditsRequestValidator : AbstractValidator<SearchAllAuditsRequest>
{
    public SearchAllAuditsRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.OrganizationId)
            .IsEntityId(identifierFactory)
            .When(req => req.OrganizationId.HasValue())
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}