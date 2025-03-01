using System.Net.Http.Headers;
using Application.Interfaces.Resources;
using Common.Extensions;

namespace Application.Resources.Shared;

public class Image : IIdentifiableResource
{
    public required string ContentType { get; set; }

    public string? Description { get; set; }

    public string? Filename { get; set; }

    public required string Url { get; set; }

    public required string Id { get; set; }
}

public class FileUpload
{
    public required Stream Content { get; set; }

    public required FileUploadContentType ContentType { get; set; }

    public string? Filename { get; set; }

    public long Size { get; set; }
}

public class FileUploadContentType
{
    public string? Charset { get; set; }

    public string? MediaType { get; set; }

    public static FileUploadContentType FromContentType(string contentType)
    {
        if (contentType.HasNoValue())
        {
            return new FileUploadContentType();
        }

        if (MediaTypeHeaderValue.TryParse(contentType, out var parsed))
        {
            return new FileUploadContentType
            {
                MediaType = parsed.MediaType,
                Charset = parsed.CharSet
            };
        }

        return new FileUploadContentType();
    }
}

public class ImageDownload
{
    public required string ContentType { get; set; }

    public required Stream Stream { get; set; }
}