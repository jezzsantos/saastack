using Domain.Common.Identity;
using EndUsersInfrastructure.Api.EndUsers;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using UnitTesting.Common.Validation;
using Xunit;

namespace EndUsersInfrastructure.UnitTests.Api.EndUsers;

[Trait("Category", "Unit")]
public class AssignPlatformRolesRequestValidatorSpec
{
    private readonly AssignPlatformRolesRequest _dto;
    private readonly AssignPlatformRolesRequestValidator _validator;

    public AssignPlatformRolesRequestValidatorSpec()
    {
        _validator = new AssignPlatformRolesRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new AssignPlatformRolesRequest
        {
            Id = "anid",
            Roles = new List<string> { "arole" }
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenRolesIsNull_ThenThrows()
    {
        _dto.Roles = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AssignPlatformRolesRequestValidator_InvalidRoles);
    }

    [Fact]
    public void WhenRolesIsEmpty_ThenThrows()
    {
        _dto.Roles = new List<string>();

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AssignPlatformRolesRequestValidator_InvalidRoles);
    }

    [Fact]
    public void WhenRoleIsInvalid_ThenThrows()
    {
        _dto.Roles = new List<string> { "aninvalidrole^" };

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AssignPlatformRolesRequestValidator_InvalidRole);
    }
}