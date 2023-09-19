using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class GetCarRequestValidator : AbstractValidator<GetCarRequest>
{
    public GetCarRequestValidator()
    {
        RuleFor(req => req.Id).NotEmpty().Matches(@"[\d]{1,3}");
    }
}