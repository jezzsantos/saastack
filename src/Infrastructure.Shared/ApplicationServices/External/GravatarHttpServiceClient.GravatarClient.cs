using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Gravatar;

namespace Infrastructure.Shared.ApplicationServices.External;

public interface IGravatarClient
{
    Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);
}

internal class GravatarClient : IGravatarClient
{
    internal const string DefaultImageBehaviour = "404";
    private const string BaseUrlSettingName = "ApplicationServices:Gravatar:BaseUrl";
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public GravatarClient(IRecorder recorder, IConfigurationSettings settings, IHttpClientFactory httpClientFactory)
        : this(recorder, settings.GetString(BaseUrlSettingName), httpClientFactory)
    {
    }

    internal GravatarClient(IRecorder recorder, IServiceClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    private GravatarClient(IRecorder recorder, string baseUrl,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new ApiServiceClient(httpClientFactory, JsonSerializerOptions.Default, baseUrl))
    {
    }

    public async Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        Result<BinaryResponse, ResponseProblem> response;
        try
        {
            response = await _serviceClient.GetBinaryAsync(caller,
                new GravatarGetImageRequest
                {
                    Hash = HashEmailAddress(emailAddress),
                    Default = DefaultImageBehaviour,
                    Width = 400
                }, null, cancellationToken);
            if (response.IsFailure)
            {
                return Optional<FileUpload>.None;
            }
        }
        catch (HttpRequestException ex)
        {
            _recorder.TraceError(caller.ToCall(), ex,
                "Error retrieving gravatar for {EmailAddress}", emailAddress);
            return Optional<FileUpload>.None;
        }

        if (response.Value.StatusCode != HttpStatusCode.OK)
        {
            return Optional<FileUpload>.None;
        }

        var contentType = response.Value.ContentType;
        var content = response.Value.Content;
        var contentLength = response.Value.ContentLength;

        return new FileUpload
        {
            ContentType = FileUploadContentType.FromContentType(contentType),
            Content = content,
            Filename = null,
            Size = contentLength
        }.ToOptional();
    }

    private static string HashEmailAddress(string emailAddress)
    {
        var inputBytes = Encoding.ASCII.GetBytes(emailAddress.ToLower().Trim());
        var hashBytes = MD5.HashData(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}