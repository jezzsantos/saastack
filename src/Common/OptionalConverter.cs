using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Common;

/// <summary>
///     Handles JSON serialization/deserialization for <see cref="Optional{T}" /> data
/// </summary>
public sealed class OptionalConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo;
        return new Optional<T>(
            reader.TokenType is JsonTokenType.Null
                ? default
                : (typeInfo = options.GetTypeInfo(typeof(T))) is JsonTypeInfo<T?>
                    ? JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)typeInfo)
                    : (T?)JsonSerializer.Deserialize(ref reader, typeInfo));
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case { HasValue: false }:
                writer.WriteNullValue();
                break;

            default:
                var typeInfo = options.GetTypeInfo(typeof(T));
                if (typeInfo is JsonTypeInfo<T?> typed)
                {
                    JsonSerializer.Serialize(writer, value.ValueOrDefault, typed);
                }
                else
                {
                    JsonSerializer.Serialize(writer, value.ValueOrDefault, typeInfo);
                }

                break;
        }
    }
}