using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsInfrastructure.Api.Organizations;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.Api.Organizations;

[Trait("Category", "Unit")]
public class InviteMemberToOrganizationRequestValidatorSpec
{
    private readonly InviteMemberToOrganizationRequest _dto;
    private readonly InviteMemberToOrganizationRequestValidator _validator;

    public InviteMemberToOrganizationRequestValidatorSpec()
    {
        _validator = new InviteMemberToOrganizationRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new InviteMemberToOrganizationRequest
        {
            Id = "anid",
            UserId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenUserIdAndEmailMissing_ThenThrows()
    {
        _dto.UserId = null;
        _dto.Email = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InviteOrganizationMemberRequestValidator_MissingUserIdAndEmail);
    }

    [Fact]
    public void WhenEmailIsInvalid_ThenThrows()
    {
        _dto.UserId = null;
        _dto.Email = "notanemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InviteOrganizationMemberRequestValidator_InvalidUserEmail);
    }

    [Fact]
    public void WhenEmailIsValid_ThenSucceeds()
    {
        _dto.UserId = null;
        _dto.Email = "auser@company.com";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmailAndUserIdIsValid_ThenSucceeds()
    {
        _dto.UserId = "anid";
        _dto.Email = "auser@company.com";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InviteOrganizationMemberRequestValidator_BothUserIdAndEmail);
    }
}