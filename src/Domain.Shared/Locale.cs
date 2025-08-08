using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Validations;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class Locale : SingleValueObjectBase<Locale, Bcp47Locale>
{
    public static readonly Locale Default = Create(Locales.Default).Value;

    public static Result<Locale, Error> Create(string value)
    {
        if (value.IsNotValuedParameter(nameof(value), out var error))
        {
            return error;
        }

        if (value.IsInvalidParameter(CommonValidations.Locale, nameof(value),
                Resources.Locale_InvalidLocale, out var error2))
        {
            return error2;
        }

        return new Locale(Locales.FindOrDefault(value));
    }

    public static Result<Locale, Error> Create(Bcp47Locale value)
    {
        return new Locale(value);
    }

    private Locale(Bcp47Locale value) : base(value)
    {
    }

    public Bcp47Locale Code => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Locale> Rehydrate()
    {
        return (property, _) => new Locale(Locales.FindOrDefault(property));
    }
}