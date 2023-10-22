using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;
using FluentValidation.Validators;

namespace Infrastructure.Web.Api.Common.Validation;

/// <summary>
///     Validates an email address
/// </summary>
internal class EmailAddressValidator<T, TProperty> : PropertyValidator<T, TProperty>
{
    public override string Name => nameof(EmailAddressValidator<T, TProperty>);

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

        return propertyValue.IsMatchWith(CommonValidations.EmailAddress.Expression!);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return ValidationResources.EmailAddressValidator_InvalidEmailAddress;
    }
}