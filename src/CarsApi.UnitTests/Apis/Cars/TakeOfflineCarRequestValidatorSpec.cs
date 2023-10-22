using CarsApi.Apis.Cars;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

[Trait("Category", "Unit")]
public class TakeOfflineCarRequestValidatorSpec
{
    private readonly TakeOfflineCarRequest _dto;
    private readonly TakeOfflineCarRequestValidator _validator;

    public TakeOfflineCarRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new TakeOfflineCarRequestValidator(idFactory.Object);
        _dto = new TakeOfflineCarRequest
        {
            Id = "anid",
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            EndAtUtc = DateTime.UtcNow.AddHours(2)
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenReasonIsInvalid_ThenThrows()
    {
        _dto.Reason = "aninvalidareason^";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.TakeOfflineCarRequestValidator_InvalidReason);
    }

    [Fact]
    public void WhenStartAtUtcIsPast_ThenThrows()
    {
        _dto.StartAtUtc = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.TakeOfflineCarRequestValidator_InvalidStartAtUtc);
    }

    [Fact]
    public void WhenEndAtUtcIsPast_ThenThrows()
    {
        _dto.EndAtUtc = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.TakeOfflineCarRequestValidator_InvalidEndAtUtc);
    }

    [Fact]
    public void WhenEndAtUtcIsLessThanStartAtUtc_ThenThrows()
    {
        _dto.StartAtUtc = DateTime.UtcNow.Add(TimeSpan.FromHours(2));
        _dto.EndAtUtc = DateTime.UtcNow.Add(TimeSpan.FromHours(1));

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.TakeOfflineCarRequestValidator_InvalidEndAtUtc);
    }
}