using BookingsInfrastructure.Api.Bookings;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using FluentValidation;
using Infrastructure.Web.Api.Interfaces.Operations.Bookings;
using Moq;
using Xunit;

namespace BookingsInfrastructure.UnitTests.Api.Bookings;

[Trait("Category", "Unit")]
public class CancelBookingRequestValidatorSpec
{
    private readonly CancelBookingRequest _dto;
    private readonly CancelBookingRequestValidator _validator;

    public CancelBookingRequestValidatorSpec()
    {
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _validator = new CancelBookingRequestValidator(idFactory.Object);
        _dto = new CancelBookingRequest
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