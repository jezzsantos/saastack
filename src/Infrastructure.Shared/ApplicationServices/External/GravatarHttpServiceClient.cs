using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides an adapter to the Gravatar.com service
///     <see href="https://docs.gravatar.com/general/images/" />
/// </summary>
public sealed class GravatarHttpServiceClient : IAvatarService
{
    private readonly IRecorder _recorder;
    private readonly IGravatarClient _serviceClient;

    public GravatarHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new GravatarClient(recorder, settings, httpClientFactory))
    {
    }

    internal GravatarHttpServiceClient(IRecorder recorder, IGravatarClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        var refId = GetRandomRefId();

        _recorder.TraceInformation(caller.ToCall(), "Retrieving avatar from Gravatar for {EmailAddress}, ref: {Ref}",
            emailAddress, refId);

        var gravatar =
            await _serviceClient.FindAvatarAsync(caller, emailAddress, cancellationToken);
        if (gravatar.IsFailure
            || !gravatar.Value.HasValue)
        {
            _recorder.TraceInformation(caller.ToCall(),
                "No gravatar image found for {EmailAddress}, ref: {Ref}", emailAddress, refId);
            return Optional<FileUpload>.None;
        }

        _recorder.TraceInformation(caller.ToCall(), "Retrieved gravatar successfully, ref: {Ref}",
            refId);

        return gravatar.Value.Value.ToOptional();
    }

    private static string GetRandomRefId()
    {
        return Guid.NewGuid().ToString("N");
    }
}