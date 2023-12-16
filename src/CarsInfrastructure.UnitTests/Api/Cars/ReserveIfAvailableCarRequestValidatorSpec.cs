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
public class ReserveCarIfAvailableRequestValidatorSpec
{
    private readonly ReserveCarIfAvailableRequest _dto;
    private readonly ReserveCarIfAvailableRequestValidator _validator;

    public ReserveCarIfAvailableRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new ReserveCarIfAvailableRequestValidator(idFactory.Object);
        _dto = new ReserveCarIfAvailableRequest
        {
            Id = "anid",
            FromUtc = DateTime.UtcNow.AddHours(1),
            ToUtc = DateTime.UtcNow.AddHours(2),
            ReferenceId = "areferenceid"
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
            .WithMessageLike(Resources.ReserveCarIfAvailableRequestValidator_InvalidFromUtc);
    }

    [Fact]
    public void WhenToUtcIsPast_ThenThrows()
    {
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ReserveCarIfAvailableRequestValidator_InvalidToUtc);
    }

    [Fact]
    public void WhenToUtcIsLessThanFromUtc_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow.AddHours(2);
        _dto.ToUtc = DateTime.UtcNow.AddHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ReserveCarIfAvailableRequestValidator_InvalidToUtc);
    }

    [Fact]
    public void WhenReferenceIdIsEmpty_ThenThrows()
    {
        _dto.ReferenceId = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ReserveCarIfAvailableRequestValidator_InvalidReferenceId);
    }

    [Fact]
    public void WhenReferenceIdIsInvalid_ThenThrows()
    {
        _dto.ReferenceId = "^aninvalidreference^";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ReserveCarIfAvailableRequestValidator_InvalidReferenceId);
    }
}