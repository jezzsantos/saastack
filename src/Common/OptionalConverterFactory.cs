using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common;

/// <summary>
///     Provides a factory to create <see cref="OptionalConverter{T}" /> instances.
/// </summary>
public sealed class OptionalConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return Optional.IsOptionalType(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Optional.TryGetContainedType(typeToConvert, out var containedType)
            ? Activator.CreateInstance(typeof(OptionalConverter<>).MakeGenericType(containedType!)) as JsonConverter
            : null;
    }
}