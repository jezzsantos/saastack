using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsInfrastructure.Api.Organizations;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.Api.Organizations;

[Trait("Category", "Unit")]
public class ChangeOrganizationRequestValidatorSpec
{
    private readonly ChangeOrganizationRequest _dto;
    private readonly ChangeOrganizationRequestValidator _validator;

    public ChangeOrganizationRequestValidatorSpec()
    {
        _validator = new ChangeOrganizationRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChangeOrganizationRequest
        {
            Id = "anid",
            Name = "aname"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNameIsInvalid_ThenThrows()
    {
        _dto.Name = "aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeOrganizationRequestValidator_InvalidName);
    }
}