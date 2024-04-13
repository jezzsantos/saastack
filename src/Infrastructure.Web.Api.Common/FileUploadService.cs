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
    internal const string UnknownContentType = "unknown";
    internal static readonly byte[] ImageJpegMagicBytes = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] ImageGif87MagicBytes = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61];
    private static readonly byte[] ImageGif89MagicBytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    private static readonly byte[] ImagePngMagicBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly IReadOnlyList<KnownFile> DetectableFileTypes = new[]
    {
        new KnownFile(HttpContentTypes.ImageJpeg, ImageJpegMagicBytes, "jpg"),
        new KnownFile(HttpContentTypes.ImageGif, ImageGif87MagicBytes, "gif"),
        new KnownFile(HttpContentTypes.ImageGif, ImageGif89MagicBytes, "gif"),
        new KnownFile(HttpContentTypes.ImagePng, ImagePngMagicBytes, "png")
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
        var declaredContentType = upload.ContentType;
        var detected = DetectFileContent(content, declaredContentType, allowableContentTypes);
        if (!detected.IsAllowed)
        {
            return Error.Validation(Resources.FileUploadService_DisallowedFileContent
                .Format(declaredContentType.HasValue()
                    ? declaredContentType
                    : UnknownContentType));
        }

        var detectedContentType = detected.ContentType;
        if (declaredContentType.HasValue() && detectedContentType.HasValue()
                                           && detectedContentType.NotEqualsIgnoreCase(declaredContentType))
        {
            return Error.Validation(
                Resources.FileUploadService_InvalidContentTypeForFileType.Format(detectedContentType,
                    declaredContentType));
        }

        if (declaredContentType.HasNoValue()
            && detected.ContentType.HasValue())
        {
            upload.ContentType = detected.ContentType;
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

    private static (bool IsAllowed, string ContentType, string? FileExtension) DetectFileContent(Stream content,
        string declaredContentType,
        IReadOnlyList<string> allowableContentTypes)
    {
        if (allowableContentTypes.HasNone())
        {
            return (false, declaredContentType, null);
        }

        foreach (var detectableFileType in DetectableFileTypes)
        {
            if (IsContentType(content, detectableFileType.MagicBytes))
            {
                if (allowableContentTypes.Contains(detectableFileType.ContentType))
                {
                    return (true, detectableFileType.ContentType, detectableFileType.FileExtension);
                }
            }
        }

        if (declaredContentType.HasValue()
            && allowableContentTypes.Contains(declaredContentType))
        {
            return (true, declaredContentType, null);
        }

        return (false, HttpContentTypes.OctetStream, null);
    }

    private static bool IsContentType(Stream content, byte[] magicBytes)
    {
        var maxBufferSize = DetectableFileTypes.Max(kf => kf.MagicBytes.Length);
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

    private record KnownFile(string ContentType, byte[] MagicBytes, string FileExtension);
}