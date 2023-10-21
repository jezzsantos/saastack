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

    public static Validation Anything(int min = 1, int max = 100)
    {
        return new Validation(@".*", min, max);
    }

    public static Validation DescriptiveName(int min = 1, int max = 100)
    {
        return new Validation(@"^[\d\w\`\!\@\#\$\%\&\(\)\-\:\;\'\,\.\?\/ ]*$", min, max);
    }

    public static Validation FreeformText(int min = 1, int max = 1000)
    {
        return new Validation(@$"^[${FreeFormTextAllowedCharacters}]*$", min, max);
    }

    public static Validation Markdown(int min = 1, int max = 1000)
    {
        return new Validation($@"^[${FreeFormTextAllowedCharacters}${Emojis}]*$", min, max);
    }

    public static bool Matches<TValue>(this Validation<TValue> format, TValue value)
    {
        if (format.Function.Exists())
        {
            return format.Function!(value);
        }

        if (value.NotExists() || format.Expression.NotExists())
        {
            return false;
        }

        if (IsInvalidLength(format, value))
        {
            return false;
        }

        var valueToMatch = value.ToString() ?? string.Empty;

        return valueToMatch.IsMatchWith(format.Expression!);
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
}