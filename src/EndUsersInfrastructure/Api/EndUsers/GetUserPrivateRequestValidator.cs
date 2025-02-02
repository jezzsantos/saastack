using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.EndUsers;

public class GetUserPrivateRequestValidator : AbstractValidator<GetUserPrivateRequest>
{
    public GetUserPrivateRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}