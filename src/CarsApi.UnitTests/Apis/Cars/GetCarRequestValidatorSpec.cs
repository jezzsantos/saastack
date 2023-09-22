using CarsApi.Apis.Cars;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using Moq;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

[Trait("Category", "Unit")]
public class GetCarRequestValidatorSpec
{
    private readonly GetCarRequest _dto;
    private readonly GetCarRequestValidator _validator;

    public GetCarRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new GetCarRequestValidator(idFactory.Object);
        _dto = new GetCarRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}