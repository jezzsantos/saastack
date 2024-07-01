using Application.Resources.Shared;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Signings;
using SigningsInfrastructure.Api.Signings;
using UnitTesting.Common.Validation;
using Xunit;

namespace SigningsInfrastructure.UnitTests.Api.Signings;

[Trait("Category", "Unit")]
public class CreateDraftSigningRequestRequestValidatorSpec
{
    private readonly CreateDraftSigningRequestRequest _dto;
    private readonly CreateDraftSigningRequestRequestValidator _validator;

    public CreateDraftSigningRequestRequestValidatorSpec()
    {
        _validator = new CreateDraftSigningRequestRequestValidator();
        _dto = new CreateDraftSigningRequestRequest
        {
            Signees =
            [
                new Signee
                {
                    EmailAddress = "auser@company.com",
                    PhoneNumber = "+6498876986"
                }
            ]
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenSigneesISEmpty_ThenThrows()
    {
        _dto.Signees = [];

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.SigneeValidator_EmptySignees);
    }
}