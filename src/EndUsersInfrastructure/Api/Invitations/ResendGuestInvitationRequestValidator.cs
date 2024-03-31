using EndUsersDomain;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;

namespace EndUsersInfrastructure.Api.Invitations;

public class ResendGuestInvitationRequestValidator : AbstractValidator<ResendGuestInvitationRequest>
{
    public ResendGuestInvitationRequestValidator()
    {
        RuleFor(req => req.Token)
            .Matches(Validations.Invitation.Token)
            .WithMessage(Resources.ResendGuestInvitationRequestValidator_InvalidToken);
    }
}