using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;

namespace Infrastructure.Web.Api.Common.Validation;

/// <summary>
///     Provides a validator for additional data in dictionary form
/// </summary>
public class AdditionalValidator : AbstractValidator<Dictionary<string, object?>?>
{
    public AdditionalValidator()
    {
        When(req => req.Exists(), () =>
        {
            RuleForEach(item => item)
                .SetValidator(new AdditionalItemValidator());
        });
    }
}

/// <summary>
///     Provides a validator for additional data in dictionary form
/// </summary>
public class AdditionalItemValidator : AbstractValidator<KeyValuePair<string, object?>>
{
    public AdditionalItemValidator()
    {
        RuleFor(item => item.Key)
            .NotEmpty()
            .WithMessage(ValidationResources.AdditionalValidator_InvalidName);
        RuleFor(item => item.Value)
            .Must(val => CommonValidations.Recording.AdditionalStringValue.Matches(val!.ToString()))
            .When(item => item.Value.Exists())
            .WithMessage(ValidationResources.AdditionalValidator_InvalidStringValue);
    }
}