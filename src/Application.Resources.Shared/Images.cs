using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Image : IIdentifiableResource
{
    public required string ContentType { get; set; }

    public required string Description { get; set; }

    public required string Filename { get; set; }

    public required string Url { get; set; }

    public required string Id { get; set; }
}

public class FileUpload
{
    public required Stream Content { get; set; }

    public required string ContentType { get; set; }

    public string? Filename { get; set; }

    public long Size { get; set; }
}

public class ImageDownload
{
    public required string ContentType { get; set; }

    public required Stream Stream { get; set; }
}