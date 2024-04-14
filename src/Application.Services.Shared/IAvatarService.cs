using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for retrieving a default avatar for an identity
/// </summary>
public interface IAvatarService
{
    /// <summary>
    ///     Returns the avatar for the specified <see cref="emailAddress" />
    /// </summary>
    Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);
}