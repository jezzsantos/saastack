using Common;
using Common.Extensions;

namespace Domain.Interfaces.Validations;

/// <summary>
///     Well known validations
/// </summary>
public static class CommonValidations
{
    public static readonly Validation CountryCode = new(CountryCodes.Exists);
    public static readonly Validation EmailAddress = new(
        @"^(?:[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+\.)*[\w\!\#\$\%\&\'\*\+\-\/\=\?\^\`\{\|\}\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!\.)){0,61}[a-zA-Z0-9]?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\[(?:(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\.){3}(?:[01]?\d{1,2}|2[0-4]\d|25[0-5])\]))$");
    public static readonly Validation FeatureLevel = new(@"^[\w\d]{4,30}$", 4, 30);
    public static readonly Validation Identifier = new(@"^[\w]{1,20}_[\d\w]{10,22}$", 12, 43);
    public static readonly Validation IdentifierPrefix = new(@"^[^\W_]*$", 1, 20);
    public static readonly Validation Role = new(@"^[\w\d]{4,30}$", 4, 30);
    public static readonly Validation Timezone = new(Timezones.Exists);
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

    public static class Passwords
    {
        public static readonly Validation PasswordHash =
            new(@"^[$]2[abxy]?[$](?:0[4-9]|[12][0-9]|3[01])[$][./0-9a-zA-Z]{53}$", 60, 60);

        public static class Password
        {
            public static readonly int MaxLength = 200;
            public static readonly int MinLength = 8;
            /// <summary>
            ///     Loose policy requires that the password contains any character, and matches length
            ///     requirements.
            /// </summary>
            public static readonly Validation Loose = new(
                @"^[\w\d \!""\#\$\%\&\'\(\)\*\+\,\-\.\/\:\;\<\=\>\?\@\[\]\^_\`\{\|\}\~]*$", MinLength, MaxLength);

            /// <summary>
            ///     Strict policy requires that the password contains at least 3 of the 4 character classes, and matches length
            ///     requirements.
            ///     The three character classes are:
            ///     1. at least one uppercase character (including unicode)
            ///     2. at least one lowercase character (including unicode)
            ///     3. at least one number character (ie. 0123456789 )
            ///     4. at least one special character (ie: <![CDATA[`~!@#$%^&*()-_=+[{]}\;:'",<.>/?]]> )
            /// </summary>
            public static readonly Validation Strict = new(password =>
            {
                if (!password.HasValue())
                {
                    return false;
                }

                if (password.Length < MinLength)
                {
                    return false;
                }

                if (password.Length > MaxLength)
                {
                    return false;
                }

                var characterClassCount = 0;
                if (password.IsMatchWith(@"[\d]{1,}"))
                {
                    characterClassCount++;
                }

                if (password.IsMatchWith(@"[\p{Ll}]{1,}"))
                {
                    characterClassCount++;
                }

                if (password.IsMatchWith(@"[\p{Lu}]{1,}"))
                {
                    characterClassCount++;
                }

                if (password.IsMatchWith(@"[ \!""\#\$\%\&\'\(\)\*\+\,\-\.\/\:\;\<\=\>\?\@\[\]\^_\`\{\|\}\~]{1,}"))
                {
                    characterClassCount++;
                }

                return characterClassCount >= 3;
            });
        }
    }
}