using BookingsDomain;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Bookings;
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

        RuleFor(req => req)
            .Must(req => req.EndUtc > req.StartUtc)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(req => req.EndUtc.HasValue);
        RuleFor(req => req)
            .Must(req => req.EndUtc?.Subtract(req.StartUtc) >= Validations.Booking.MinimumBookingDuration)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(req => req.EndUtc.HasValue);
        RuleFor(req => req)
            .Must(req => req.EndUtc?.Subtract(req.StartUtc) <= Validations.Booking.MaximumBookingDuration)
            .WithMessage(Resources.MakeBookingRequestValidator_InvalidEndUtc)
            .When(req => req.EndUtc.HasValue);
    }
}