#if TESTINGONLY
using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using JetBrains.Annotations;

namespace ApiHost1.Api.TestingOnly;

[UsedImplicitly]
public class
    ValidationsValidatedPostTestingOnlyRequestValidator : AbstractValidator<ValidationsValidatedPostTestingOnlyRequest>
{
    public ValidationsValidatedPostTestingOnlyRequestValidator()
    {
        RuleFor(req => req.Id)
            .NotEmpty()
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidId);
        RuleFor(req => req.RequiredField)
            .NotEmpty()
            .Matches(@"[\d]{1,3}")
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidRequiredField);
        RuleFor(req => req.OptionalField)
            .NotEmpty()
            .Matches(@"[\d]{1,3}")
            .When(req => req.OptionalField.HasValue())
            .WithMessage(Resources.GetTestingOnlyValidatedRequestValidator_InvalidOptionalField);
    }
}
#endif