using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Invitations;

public class InviteGuestRequestValidator : AbstractValidator<InviteGuestRequest>
{
    public InviteGuestRequestValidator()
    {
        RuleFor(req => req.Email)
            .IsEmailAddress()
            .WithMessage(Resources.InviteGuestRequestValidator_InvalidEmail);
    }
}