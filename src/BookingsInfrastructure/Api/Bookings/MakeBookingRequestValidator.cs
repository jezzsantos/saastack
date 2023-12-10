using BookingsDomain;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Bookings;
using JetBrains.Annotations;

namespace BookingsInfrastructure.Api.Bookings;

[UsedImplicitly]
public class MakeBookingRequestValidator : AbstractValidator<MakeBookingRequest>
{
    public MakeBookingRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.CarId)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.StartUtc)
            .GreaterThan(req => DateTime.UtcNow)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidStartUtc);

        RuleFor(dto => dto)
            .Must(dto => dto.EndUtc > dto.StartUtc)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(dto => dto.EndUtc.HasValue);
        RuleFor(dto => dto)
            .Must(dto => dto.EndUtc?.Subtract(dto.StartUtc) >= Validations.Booking.MinimumBookingDuration)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(dto => dto.EndUtc.HasValue);
        RuleFor(dto => dto)
            .Must(dto => dto.EndUtc?.Subtract(dto.StartUtc) <= Validations.Booking.MaximumBookingDuration)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(dto => dto.EndUtc.HasValue);
    }
}