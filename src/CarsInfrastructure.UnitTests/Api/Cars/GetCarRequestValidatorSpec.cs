using CarsInfrastructure.Api.Cars;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using Moq;
using Xunit;

namespace CarsInfrastructure.UnitTests.Api.Cars;

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