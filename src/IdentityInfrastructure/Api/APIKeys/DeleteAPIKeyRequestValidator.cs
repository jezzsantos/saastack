using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;

namespace IdentityInfrastructure.Api.APIKeys;

[UsedImplicitly]
public class DeleteAPIKeyRequestValidator : AbstractValidator<DeleteAPIKeyRequest>
{
    public DeleteAPIKeyRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}