using Common.Extensions;

namespace Domain.Interfaces.Validations;

/// <summary>
///     Well known validations
/// </summary>
public static class CommonValidations
{
    public static readonly Validation EmailAddress = new(
        @"^(?:[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+\.)*[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!\.)){0,61}[a-zA-Z0-9]?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\[(?:(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\.){3}(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\]))$");

    public static readonly Validation Identifier = new(@"^[\w]{1,20}_[\d\w]{10,22}$", 12, 43);

    public static readonly Validation IdentifierPrefix = new(@"^[^\W_]*$", 1, 20);

    public static readonly Validation Url = new(s => Uri.IsWellFormedUriString(s, UriKind.Absolute));

    private static readonly string Emojis =
        "😀😁😂😃😉😋😎😍😗🤗🤔😣😫😴😌🤓😛😜😠😇😷😈👻😺😸😹😻😼😽🙀🙈🙉🙊👼👮🕵💂👳🎅👸👰👲🙍🙇🚶🏃💃⛷🏂🏌🏄🚣🏊⛹🏋🚴👫💪👈👉👆🖕👇🖖🤘🖐👌👍👎✊👊👏🙌🙏🐵🐶🐇🐥🐸🐌🐛🐜🐝🍉🍄🍔🍤🍨🍪🎂🍰🍾🍷🍸🍺🌍🚑⏰🌙🌝🌞⭐🌟🌠🌨🌩⛄🔥🎄🎈🎉🎊🎁🎗🏀🏈🎲🔇🔈📣🔔🎵🎷💰🖊📅✅❎💯";

    private static readonly string FreeFormTextAllowedCharacters =
        @"\d\w\`\~\!\@\#\$\%\:\&\*\(\)\-\+\=\[\]\{{\}}\:\;\'\""\<\,\>\.\?\|\/ \r\n";

    /// <summary>
    ///     Validation for any text
    /// </summary>
    public static Validation Anything(int min = 1, int max = 100)
    {
        return new Validation(@".*", min, max);
    }

    /// <summary>
    ///     Validation for any written descriptive name
    /// </summary>
    public static Validation DescriptiveName(int min = 1, int max = 100)
    {
        return new Validation(@"^[\d\w\`\!\@\#\$\%\&\(\)\-\:\;\'\,\.\?\/ ]*$", min, max);
    }

    /// <summary>
    ///     Validation for any freeform text (almost any character)
    /// </summary>
    public static Validation FreeformText(int min = 1, int max = 1000)
    {
        return new Validation(@$"^[${FreeFormTextAllowedCharacters}]*$", min, max);
    }

    /// <summary>
    ///     Validation for any Markdown editor text
    /// </summary>
    public static Validation Markdown(int min = 1, int max = 1000)
    {
        return new Validation($@"^[${FreeFormTextAllowedCharacters}${Emojis}]*$", min, max);
    }

    /// <summary>
    ///     Whether the specified  <see cref="value" /> matches the specified <see cref="validation" />
    /// </summary>
    public static bool Matches<TValue>(this Validation<TValue> validation, TValue value)
    {
        if (validation.Function.Exists())
        {
            return validation.Function!(value);
        }

        if (value.NotExists() || validation.Expression.NotExists())
        {
            return false;
        }

        if (IsInvalidLength(validation, value))
        {
            return false;
        }

        var valueToMatch = value.ToString() ?? string.Empty;

        return valueToMatch.IsMatchWith(validation.Expression!);
    }

    private static bool IsInvalidLength<TValue>(Validation<TValue> format, TValue value)
    {
        if (value.NotExists())
        {
            return true;
        }

        if (format.MinLength.HasValue && value.ToString()!.Length < format.MinLength.Value)
        {
            return true;
        }

        if (format.MaxLength.HasValue && value.ToString()!.Length > format.MaxLength.Value)
        {
            return true;
        }

        return false;
    }

    public static class Recording
    {
        public static readonly Validation AdditionalStringValue = DescriptiveName(1, 300);
    }
}