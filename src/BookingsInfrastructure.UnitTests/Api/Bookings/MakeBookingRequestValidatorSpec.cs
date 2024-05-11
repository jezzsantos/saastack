using BookingsDomain;
using BookingsInfrastructure.Api.Bookings;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Bookings;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace BookingsInfrastructure.UnitTests.Api.Bookings;

[Trait("Category", "Unit")]
public class MakeBookingRequestValidatorSpec
{
    private readonly MakeBookingRequest _dto;
    private readonly MakeBookingRequestValidator _validator;

    public MakeBookingRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new MakeBookingRequestValidator(idFactory.Object);
        _dto = new MakeBookingRequest
        {
            CarId = "acarid",
            StartUtc = DateTime.UtcNow.AddHours(1),
            EndUtc = DateTime.UtcNow.AddHours(2)
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenStartUtcIsPast_ThenThrows()
    {
        _dto.StartUtc = DateTime.UtcNow.SubtractSeconds(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MakeBookingRequestValidator_InvalidStartUtc);
    }

    [Fact]
    public void WhenEndUtcIsBeforeStartUtc_ThenThrows()
    {
        _dto.StartUtc = DateTime.UtcNow.AddHours(1);
        _dto.EndUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MakeBookingRequestValidator_InvalidEndUtc);
    }

    [Fact]
    public void WhenDurationIsTooShort_ThenThrows()
    {
        _dto.EndUtc =
            _dto.StartUtc!.Value.Add(Validations.Booking.MinimumBookingDuration.Subtract(TimeSpan.FromSeconds(1)));

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MakeBookingRequestValidator_InvalidEndUtc);
    }

    [Fact]
    public void WhenDurationIsTooLong_ThenThrows()
    {
        _dto.EndUtc =
            _dto.StartUtc!.Value.Add(Validations.Booking.MaximumBookingDuration.Add(TimeSpan.FromSeconds(1)));

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.MakeBookingRequestValidator_InvalidEndUtc);
    }
}