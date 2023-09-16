using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;

namespace CarsApi.Apis.Cars;

public class GetCarRequestValidator : AbstractValidator<GetCarRequest>
{
    public GetCarRequestValidator()
    {
        RuleFor(req => req.Id).NotEmpty().Matches(@"[\d]{1,3}");
    }
}