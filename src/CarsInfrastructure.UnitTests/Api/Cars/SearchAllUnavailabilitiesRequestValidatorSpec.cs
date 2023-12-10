#if TESTINGONLY
using CarsInfrastructure.Api.Cars;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using Moq;
using Xunit;

namespace CarsInfrastructure.UnitTests.Api.Cars;

[Trait("Category", "Unit")]
public class SearchAllUnavailabilitiesRequestValidatorSpec
{
    private readonly SearchAllCarUnavailabilitiesRequest _dto;
    private readonly SearchAllCarUnavailabilitiesRequestValidator _validator;

    public SearchAllUnavailabilitiesRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator =
            new SearchAllCarUnavailabilitiesRequestValidator(idFactory.Object,
                new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new SearchAllCarUnavailabilitiesRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }
}
#endif