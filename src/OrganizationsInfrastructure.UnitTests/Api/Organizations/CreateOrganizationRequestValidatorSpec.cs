using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using OrganizationsInfrastructure.Api.Organizations;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.Api.Organizations;

[Trait("Category", "Unit")]
public class CreateOrganizationRequestValidatorSpec
{
    private readonly CreateOrganizationRequest _dto;
    private readonly CreateOrganizationRequestValidator _validator;

    public CreateOrganizationRequestValidatorSpec()
    {
        _validator = new CreateOrganizationRequestValidator();
        _dto = new CreateOrganizationRequest
        {
            Name = "aname"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNameIsEmpty_ThenThrows()
    {
        _dto.Name = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CreateOrganizationRequestValidator_InvalidName);
    }
}