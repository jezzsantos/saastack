using EndUsersInfrastructure.Api.Invitations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using UnitTesting.Common.Validation;
using Xunit;

namespace EndUsersInfrastructure.UnitTests.Api.Invitations;

[Trait("Category", "Unit")]
public class ResendGuestInvitationRequestValidatorSpec
{
    private readonly ResendGuestInvitationRequest _dto;
    private readonly ResendGuestInvitationRequestValidator _validator;

    public ResendGuestInvitationRequestValidatorSpec()
    {
        _validator = new ResendGuestInvitationRequestValidator();
        _dto = new ResendGuestInvitationRequest
        {
            Token = new TokensService().CreateRegistrationVerificationToken()
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenTokenIsNull_ThenThrows()
    {
        _dto.Token = null!;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.VerifyGuestInvitationRequestValidator_InvalidToken);
    }

    [Fact]
    public void WhenTokenIsInvalid_ThenThrows()
    {
        _dto.Token = "aninvalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.VerifyGuestInvitationRequestValidator_InvalidToken);
    }
}