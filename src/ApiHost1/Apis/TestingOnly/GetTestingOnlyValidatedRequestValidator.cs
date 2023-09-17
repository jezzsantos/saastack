using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

namespace ApiHost1.Apis.TestingOnly;

public class GetTestingOnlyValidatedRequestValidator : AbstractValidator<GetTestingOnlyValidatedRequest>
{
    public GetTestingOnlyValidatedRequestValidator()
    {
        RuleFor(req => req.Id)
            .Matches(@"\\d{1,3}")
            .WithMessage(Resources.GetTestingOnlyValidatedRequest2Validator_InvalidId);
    }
}