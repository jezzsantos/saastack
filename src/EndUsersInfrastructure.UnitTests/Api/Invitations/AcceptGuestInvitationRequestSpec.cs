using EndUsersInfrastructure.Api.Invitations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using UnitTesting.Common.Validation;
using Xunit;

namespace EndUsersInfrastructure.UnitTests.Api.Invitations;

[Trait("Category", "Unit")]
public class VerifyGuestInvitationRequestValidatorSpec
{
    private readonly VerifyGuestInvitationRequest _dto;
    private readonly VerifyGuestInvitationRequestValidator _validator;

    public VerifyGuestInvitationRequestValidatorSpec()
    {
        _validator = new VerifyGuestInvitationRequestValidator();
        _dto = new VerifyGuestInvitationRequest
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