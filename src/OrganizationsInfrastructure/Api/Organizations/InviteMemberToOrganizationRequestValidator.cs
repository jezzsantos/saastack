using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;

namespace OrganizationsInfrastructure.Api.Organizations;

public class InviteMemberToOrganizationRequestValidator : AbstractValidator<InviteMemberToOrganizationRequest>
{
    public InviteMemberToOrganizationRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId)
            .When(req => req.UserId.HasValue());
        RuleFor(req => req.Email)
            .IsEmailAddress()
            .WithMessage(Resources.InviteOrganizationMemberRequestValidator_InvalidUserEmail)
            .When(req => req.Email.HasValue());
        RuleFor(req => req)
            .Null()
            .WithMessage(Resources.InviteOrganizationMemberRequestValidator_MissingUserIdAndEmail)
            .When(req => req.UserId.HasNoValue() && req.Email.HasNoValue());
        RuleFor(req => req)
            .Null()
            .WithMessage(Resources.InviteOrganizationMemberRequestValidator_BothUserIdAndEmail)
            .When(req => req.UserId.HasValue() && req.Email.HasValue());
    }
}