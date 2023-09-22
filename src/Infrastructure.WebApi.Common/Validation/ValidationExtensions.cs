using Common.Extensions;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;
using FluentValidation;

namespace Infrastructure.WebApi.Common.Validation;

public static class ValidationExtensions
{
    /// <summary>
    ///     Whether the property matches the <see cref="validation" />
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> Matches<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder, Validation<TProperty> validation)
        where TProperty : notnull
    {
        return ruleBuilder.SetValidator(new ValidatorValidator<T, TProperty>(validation));
    }

    /// <summary>
    ///     Whether the property is a valid <see cref="Identifier" />
    /// </summary>
    public static IRuleBuilderOptions<TDto, string> IsEntityId<TDto>(this IRuleBuilder<TDto, string> rule,
        IIdentifierFactory identifierFactory)
    {
        return rule.Must(id => id.HasValue() && identifierFactory.IsValid(Identifier.Create(id)));
    }

    /// <summary>
    ///     Whether the property is a valid email address
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> IsEmailAddress<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new EmailAddressValidator<T, TProperty>());
    }

    /// <summary>
    ///     Whether the property is a valid URL
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> IsUrl<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder)
    {
        return ruleBuilder.SetValidator(new UrlValidator<T, TProperty>());
    }
}