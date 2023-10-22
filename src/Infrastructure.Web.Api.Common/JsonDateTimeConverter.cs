using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Extensions;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     A converter for JSON for handling <see cref="DateTime" /> conversions.
///     Reads either a ISO8601 date, or UNIX timestamp.
///     Writes a UNIX timestamp
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    private readonly DateFormat _format;

    public JsonDateTimeConverter(DateFormat format)
    {
        _format = format;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            return dateString.FromIso8601();
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var dateLong = reader.GetInt64();
            return dateLong.FromUnixTimestamp();
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        switch (_format)
        {
            case DateFormat.UnixTimestamp:
                WriteUnixSeconds(writer, value);
                return;

            case DateFormat.Iso8601:
                WriteIso8601(writer, value);
                return;

            default:
                WriteUnixSeconds(writer, value);
                return;
        }
    }

    private static void WriteUnixSeconds(Utf8JsonWriter writer, DateTime value)
    {
        var unixDate = value.ToUnixSeconds();
        writer.WriteNumberValue(unixDate);
    }

    private static void WriteIso8601(Utf8JsonWriter writer, DateTime value)
    {
        var isoDate = value.ToIso8601();
        writer.WriteStringValue(isoDate);
    }
}

/// <summary>
///     Defines the formats for JSON timestamps
/// </summary>
public enum DateFormat
{
    UnixTimestamp,
    Iso8601
}