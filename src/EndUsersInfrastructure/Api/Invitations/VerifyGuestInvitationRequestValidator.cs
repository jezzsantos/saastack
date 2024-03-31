using EndUsersDomain;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Invitations;

public class VerifyGuestInvitationRequestValidator : AbstractValidator<VerifyGuestInvitationRequest>
{
    public VerifyGuestInvitationRequestValidator()
    {
        RuleFor(req => req.Token)
            .Matches(Validations.Invitation.Token)
            .WithMessage(Resources.VerifyGuestInvitationRequestValidator_InvalidToken);
    }
}