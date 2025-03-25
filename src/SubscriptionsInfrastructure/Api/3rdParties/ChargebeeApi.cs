using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.External.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api._3rdParties;

public class ChargebeeApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IChargebeeApplication _chargebeeApplication;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRecorder _recorder;
    private readonly string _webhookPassword;
    private readonly string _webhookUsername;

    public ChargebeeApi(IRecorder recorder, IHttpContextAccessor httpContextAccessor,
        ICallerContextFactory callerFactory, IConfigurationSettings settings,
        IChargebeeApplication chargebeeApplication)
    {
        _recorder = recorder;
        _httpContextAccessor = httpContextAccessor;
        _callerFactory = callerFactory;
        _chargebeeApplication = chargebeeApplication;
        _webhookUsername = settings.Platform.GetString(ChargebeeStateInterpreter.Constants.WebhookUsernameSettingName);
        _webhookPassword = settings.Platform.GetString(ChargebeeStateInterpreter.Constants.WebhookPasswordSettingName,
            string.Empty);
    }

    /// <summary>
    ///     Authenticates the request with Chargebee Basic auth, with only username, or with password
    ///     See <see href="https://apidocs.chargebee.com/docs/api/events" />
    /// </summary>
    internal static Result<Error> AuthenticateRequest(IRecorder recorder, ICallerContext caller,
        IHttpContextAccessor httpContextAccessor, string webhookUsername, string webhookPassword)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        if (!httpContext.Request.IsHttps)
        {
            recorder.TraceWarning(caller.ToCall(), "ChargebeeApi authentication is not secured with HTTPS");
            return Error.NotAuthenticated();
        }

        if (webhookUsername.HasNoValue())
        {
            recorder.TraceWarning(caller.ToCall(), "ChargebeeApi authentication is misconfigured");
            return Error.NotAuthenticated();
        }

        var basicAuth = httpContext.Request.GetBasicAuth();
        if (!basicAuth.Username.HasValue)
        {
            recorder.Audit(caller.ToCall(), Audits.ChargebeeApi_WebhookAuthentication_Failed,
                "Chargebee webhook failed authentication");
            return Error.NotAuthenticated();
        }

        var username = basicAuth.Username.Value;
        if (username.NotEqualsOrdinal(webhookUsername))
        {
            recorder.Audit(caller.ToCall(), Audits.ChargebeeApi_WebhookAuthentication_Failed,
                "Chargebee webhook failed authentication");
            return Error.NotAuthenticated();
        }

        if (!basicAuth.Password.HasValue)
        {
            return Result.Ok;
        }

        if (webhookPassword.HasNoValue())
        {
            recorder.Audit(caller.ToCall(), Audits.ChargebeeApi_WebhookAuthentication_Failed,
                "Chargebee webhook failed authentication");
            return Error.NotAuthenticated();
        }

        var password = basicAuth.Password.Value;
        if (password.EqualsOrdinal(webhookPassword))
        {
            return Result.Ok;
        }

        recorder.Audit(caller.ToCall(), Audits.ChargebeeApi_WebhookAuthentication_Failed,
            "Chargebee webhook failed authentication");
        return Error.NotAuthenticated();
    }

    public async Task<ApiEmptyResult> NotifyWebhookEvent(ChargebeeNotifyWebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        var caller = _callerFactory.Create();
        var authenticated =
            AuthenticateRequest(_recorder, caller, _httpContextAccessor, _webhookUsername, _webhookPassword);
        if (authenticated.IsFailure)
        {
            return () => authenticated.Error;
        }

        if (request.EventType.NotExists())
        {
            return () => new EmptyResponse();
        }

        var maintenance = Caller.CreateAsMaintenance(caller.CallId);
        var notified =
            await _chargebeeApplication.NotifyWebhookEvent(maintenance, request.Id!, request.EventType, request.Content,
                cancellationToken);

        return () => notified.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
}