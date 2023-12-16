using CarsInfrastructure.Api.Cars;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace CarsInfrastructure.UnitTests.Api.Cars;

[Trait("Category", "Unit")]
public class ScheduleMaintenanceCarRequestValidatorSpec
{
    private readonly ScheduleMaintenanceCarRequest _dto;
    private readonly ScheduleMaintenanceCarRequestValidator _validator;

    public ScheduleMaintenanceCarRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new ScheduleMaintenanceCarRequestValidator(idFactory.Object);
        _dto = new ScheduleMaintenanceCarRequest
        {
            Id = "anid",
            FromUtc = DateTime.UtcNow.AddHours(1),
            ToUtc = DateTime.UtcNow.AddHours(2)
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFromUtcIsPast_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ScheduleMaintenanceCarRequestValidator_InvalidFromUtc);
    }

    [Fact]
    public void WhenToUtcIsPast_ThenThrows()
    {
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ScheduleMaintenanceCarRequestValidator_InvalidToUtc);
    }

    [Fact]
    public void WhenToUtcIsLessThanFromUtc_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.AddHours(2);
        _dto.ToUtc = DateTime.UtcNow.AddHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ScheduleMaintenanceCarRequestValidator_InvalidToUtc);
    }
}