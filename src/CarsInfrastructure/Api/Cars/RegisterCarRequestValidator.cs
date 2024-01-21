using CarsDomain;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

[UsedImplicitly]
public class RegisterCarRequestValidator : AbstractValidator<RegisterCarRequest>
{
    public RegisterCarRequestValidator()
    {
        RuleFor(req => req.Make)
            .Matches(Validations.Car.Make)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidMake);
        RuleFor(req => req.Model)
            .Matches(Validations.Car.Model)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidModel);
        RuleFor(req => req.Year)
            .InclusiveBetween(Validations.Car.Year.Min, Validations.Car.Year.Max)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidYear);
        RuleFor(req => req.Jurisdiction)
            .Matches(Validations.Car.Jurisdiction)
            .Must(req => Jurisdiction.AllowedCountries.Contains(req))
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidJurisdiction);
        RuleFor(req => req.NumberPlate)
            .Matches(Validations.Car.NumberPlate)
            .WithMessage(Resources.RegisterCarRequestValidator_InvalidNumberPlate);
    }
}