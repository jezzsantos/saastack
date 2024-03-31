using EndUsersInfrastructure.Api.Invitations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using UnitTesting.Common.Validation;
using Xunit;

namespace EndUsersInfrastructure.UnitTests.Api.Invitations;

[Trait("Category", "Unit")]
public class InviteGuestRequestValidatorSpec
{
    private readonly InviteGuestRequest _dto;
    private readonly InviteGuestRequestValidator _validator;

    public InviteGuestRequestValidatorSpec()
    {
        _validator = new InviteGuestRequestValidator();
        _dto = new InviteGuestRequest
        {
            Email = "auser@company.com"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmailIsInvalid_ThenThrows()
    {
        _dto.Email = "aninvalidemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InviteGuestRequestValidator_InvalidEmail);
    }
}