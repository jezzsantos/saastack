using Application.Resources.Shared;
using Common;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a parser for deriving file types from file content.
/// </summary>
public interface IFileUploadService
{
    Result<FileUpload, Error> GetUploadedFile(IReadOnlyList<FileUpload> uploads, long maxSizeInBytes,
        IReadOnlyList<string> allowableContentTypes);
}