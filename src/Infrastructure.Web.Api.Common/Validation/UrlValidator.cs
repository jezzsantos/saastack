using Common.Extensions;
using FluentValidation;
using FluentValidation.Validators;

namespace Infrastructure.Web.Api.Common.Validation;

/// <summary>
///     Validates a URL
/// </summary>
internal class UrlValidator<T, TProperty> : PropertyValidator<T, TProperty>
{
    public override string Name => nameof(UrlValidator<T, TProperty>);

    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        if (value.NotExists())
        {
            return false;
        }

        var propertyValue = value.ToString();
        if (propertyValue.HasNoValue())
        {
            return false;
        }

        return Uri.IsWellFormedUriString(propertyValue, UriKind.Absolute);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return ValidationResources.UrlValidator_ErrorMessage;
    }
}