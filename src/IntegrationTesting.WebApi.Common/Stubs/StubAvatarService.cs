using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IAvatarService" />
/// </summary>
public class StubAvatarService : IAvatarService
{
    public async Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Optional<FileUpload>.None;
    }
}