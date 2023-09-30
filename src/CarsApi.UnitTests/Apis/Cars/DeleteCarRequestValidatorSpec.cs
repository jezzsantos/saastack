using CarsApi.Apis.Cars;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using Moq;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

[Trait("Category", "Unit")]
public class DeleteCarRequestValidatorSpec
{
    private readonly DeleteCarRequest _dto;
    private readonly DeleteCarRequestValidator _validator;

    public DeleteCarRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new DeleteCarRequestValidator(idFactory.Object);
        _dto = new DeleteCarRequest
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