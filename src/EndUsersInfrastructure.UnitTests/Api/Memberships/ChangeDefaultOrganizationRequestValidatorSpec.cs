using Domain.Common.Identity;
using EndUsersInfrastructure.Api.Memberships;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using Xunit;

namespace EndUsersInfrastructure.UnitTests.Api.Memberships;

[Trait("Category", "Unit")]
public class ChangeDefaultOrganizationRequestValidatorSpec
{
    private readonly ChangeDefaultOrganizationRequest _dto;
    private readonly ChangeDefaultOrganizationRequestValidator _validator;

    public ChangeDefaultOrganizationRequestValidatorSpec()
    {
        _validator = new ChangeDefaultOrganizationRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChangeDefaultOrganizationRequest
        {
            OrganizationId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}