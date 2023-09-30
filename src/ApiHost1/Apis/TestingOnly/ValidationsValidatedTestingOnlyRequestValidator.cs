#if TESTINGONLY
using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using JetBrains.Annotations;

namespace ApiHost1.Apis.TestingOnly;

[UsedImplicitly]
public class ValidationsValidatedTestingOnlyRequestValidator : AbstractValidator<ValidationsValidatedTestingOnlyRequest>
{
    public ValidationsValidatedTestingOnlyRequestValidator()
    {
        RuleFor(req => req.Id)
            .NotEmpty()
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidId);
        RuleFor(req => req.Field1)
            .NotEmpty()
            .Matches(@"[\d]{1,3}")
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidField1);
        RuleFor(req => req.Field2)
            .NotEmpty()
            .Matches(@"[\d]{1,3}")
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidField2);
    }
}
#endif