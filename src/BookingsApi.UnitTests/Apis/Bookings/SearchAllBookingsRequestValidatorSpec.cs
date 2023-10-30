using BookingsApi.Apis.Bookings;
using Common.Extensions;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Bookings;
using UnitTesting.Common.Validation;
using Xunit;

namespace BookingsApi.UnitTests.Apis.Bookings;

[Trait("Category", "Unit")]
public class SearchAllBookingsRequestValidatorSpec
{
    private readonly SearchAllBookingsRequest _dto;
    private readonly SearchAllBookingsRequestValidator _validator;

    public SearchAllBookingsRequestValidatorSpec()
    {
        _validator =
            new SearchAllBookingsRequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new SearchAllBookingsRequest
        {
            FromUtc = DateTime.UtcNow.AddHours(1),
            ToUtc = DateTime.UtcNow.AddHours(2)
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFromUtcIsNull_ThenSucceeds()
    {
        _dto.FromUtc = null;
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenToUtcIsNull_ThenSucceeds()
    {
        _dto.FromUtc = DateTime.UtcNow.SubtractHours(1);
        _dto.ToUtc = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenToUtcIsLessThanFromUtc_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow;
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.SearchAllBookingsRequestValidator_InvalidToUtc);
    }
}