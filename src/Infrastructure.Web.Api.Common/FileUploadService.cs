using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Provides a service to fetch an uploaded file, and to validate its content type and size.
/// </summary>
public sealed class FileUploadService : IFileUploadService
{
    internal const string UnknownMediaType = "unknown";
    internal static readonly byte[] ImageJpegMagicBytes = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] ImageGif87MagicBytes = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61];
    private static readonly byte[] ImageGif89MagicBytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    private static readonly byte[] ImagePngMagicBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly IReadOnlyList<KnownFile> DetectableImageTypes = new[]
    {
        new KnownFile(HttpConstants.ContentTypes.ImageJpeg, ImageJpegMagicBytes, "jpg"),
        new KnownFile(HttpConstants.ContentTypes.ImageGif, ImageGif87MagicBytes, "gif"),
        new KnownFile(HttpConstants.ContentTypes.ImageGif, ImageGif89MagicBytes, "gif"),
        new KnownFile(HttpConstants.ContentTypes.ImagePng, ImagePngMagicBytes, "png")
    };

    public Result<FileUpload, Error> GetUploadedFile(IReadOnlyList<FileUpload> uploads, long maxSizeInBytes,
        IReadOnlyList<string> allowableContentTypes)
    {
        if (uploads.HasNone())
        {
            return Error.Validation(Resources.FileUploadService_NoFiles);
        }

        var upload = uploads.First();
        if (upload.Size == 0)
        {
            return Error.Validation(Resources.FileUploadService_MissingContent);
        }

        if (upload.Size > maxSizeInBytes)
        {
            return Error.Validation(
                Resources.FileUploadService_InvalidFileSize.Format(maxSizeInBytes));
        }

        var content = upload.Content;
        var declaredMediaType = upload.ContentType.Exists()
            ? upload.ContentType.MediaType
            : null;
        var detected = DetectFileContent(content, declaredMediaType, allowableContentTypes);
        if (!detected.IsAllowed)
        {
            return Error.Validation(Resources.FileUploadService_DisallowedFileContent
                .Format(declaredMediaType.HasValue()
                    ? declaredMediaType
                    : UnknownMediaType));
        }

        var detectedMediaType = detected.MediaType;
        if (declaredMediaType.HasValue()
            && detectedMediaType.HasValue()
            && declaredMediaType.NotEqualsIgnoreCase(detectedMediaType))
        {
            return Error.Validation(
                Resources.FileUploadService_InvalidContentTypeForFileType.Format(detectedMediaType,
                    declaredMediaType));
        }

        if (declaredMediaType.HasNoValue()
            && detected.MediaType.HasValue())
        {
            upload.ContentType = FileUploadContentType.FromContentType(detected.MediaType);
        }

        var detectedExtension = detected.FileExtension.HasValue()
            ? $".{detected.FileExtension}"
            : string.Empty;
        var declaredFilename = upload.Filename;
        var declaredExtension = declaredFilename.HasValue()
            ? new FileInfo(declaredFilename).Extension
            : string.Empty;
        if (declaredExtension.HasValue())
        {
            if (declaredExtension.NotEqualsIgnoreCase(detectedExtension))
            {
                return Error.Validation(Resources.FileUploadService_InvalidFileExtensionForFileType);
            }
        }
        else
        {
            if (declaredFilename.HasValue() && detectedExtension.HasValue())
            {
                upload.Filename = $"{declaredFilename}{detectedExtension}";
            }
        }

        return upload;
    }

    private static (bool IsAllowed, string? MediaType, string? FileExtension) DetectFileContent(Stream content,
        string? declaredMediaType, IReadOnlyList<string> allowableMediaTypes)
    {
        if (allowableMediaTypes.HasNone())
        {
            return (false, declaredMediaType, null);
        }

        foreach (var detectableFileType in DetectableImageTypes)
        {
            if (IsMatchingStream(content, detectableFileType.MagicBytes))
            {
                if (allowableMediaTypes.Contains(detectableFileType.MediaType))
                {
                    return (true, detectableFileType.MediaType, detectableFileType.FileExtension);
                }
            }
        }

        if (declaredMediaType.HasValue()
            && allowableMediaTypes.Contains(declaredMediaType))
        {
            return (true, declaredMediaType, null);
        }

        return (false, HttpConstants.ContentTypes.OctetStream, null);
    }

    private static bool IsMatchingStream(Stream content, byte[] magicBytes)
    {
        var maxBufferSize = DetectableImageTypes.Max(kf => kf.MagicBytes.Length);
        var buffer = new byte[maxBufferSize];
        int bytesRead;
        try
        {
            bytesRead = content.Read(buffer, 0, magicBytes.Length);
        }
        finally
        {
            content.Rewind();
        }

        var isMatch = buffer
            .Take(bytesRead)
            .SequenceEqual(magicBytes);

        return isMatch;
    }

    private record KnownFile(string MediaType, byte[] MagicBytes, string FileExtension);
}