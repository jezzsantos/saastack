using System.Text;
using System.Text.Json;
using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[Trait("Category", "Unit")]
public class JsonDateTimeConverterSpec
{
    private readonly JsonDateTimeConverter _converter = new(DateFormat.Iso8601);

    [Fact]
    public void WhenReadAndIsNeitherStringNorNumber_ThenReturnsMinDate()
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
        reader.Read();

        var result = _converter.Read(ref reader, typeof(DateTime), JsonSerializerOptions.Default);

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenReadAndIsStringWithInvalidDate_ThenReturnsMinDate()
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"notavaliddate\""));
        reader.Read();

        var result = _converter.Read(ref reader, typeof(DateTime), JsonSerializerOptions.Default);

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenReadAndIsStringAsISO8601Date_ThenReturnsDate()
    {
        var now = DateTime.UtcNow;
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes($"\"{now.ToIso8601()}\""));
        reader.Read();

        var result = _converter.Read(ref reader, typeof(DateTime), JsonSerializerOptions.Default);

        result.Should().Be(now);
    }

    [Fact]
    public void WhenReadAndIsNumberAsUnixTimestamp_ThenReturnsDate()
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes($"{now.ToUnixSeconds()}"));
        reader.Read();

        var result = _converter.Read(ref reader, typeof(DateTime), JsonSerializerOptions.Default);

        result.Should().Be(now);
    }

    [Fact]
    public void WhenWriteAndUnixTimestampFormat_ThenWriteUnixTimestamp()
    {
        var now = DateTime.UtcNow;
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var converter = new JsonDateTimeConverter(DateFormat.UnixTimestamp);
        converter.Write(writer, now, JsonSerializerOptions.Default);

        writer.Flush();
        stream.Rewind();
        var result = new StreamReader(stream).ReadToEnd();

        result.Should().Be($"{now.ToUnixSeconds()}");
    }

    [Fact]
    public void WhenWriteAndIso8601Format_ThenWriteUnixTimestamp()
    {
        var now = DateTime.UtcNow;
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var converter = new JsonDateTimeConverter(DateFormat.Iso8601);
        converter.Write(writer, now, JsonSerializerOptions.Default);

        writer.Flush();
        stream.Rewind();
        var result = new StreamReader(stream).ReadToEnd();

        result.Should().Be($"\"{now.ToIso8601()}\"");
    }
}