using Application.Common;
using Application.Persistence.Interfaces;
using Common;
using Common.Recording;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Workers.Api;

public static class ServiceClientExtensions
{
    /// <summary>
    ///     Posts the specified <see cref="message" /> to the specified API <see cref="request" />
    /// </summary>
    public static async Task PostQueuedMessageToApiOrThrowAsync<TQueuedMessage, TWebResponse>(
        this IServiceClient serviceClient, IRecorder recorder, TQueuedMessage message,
        IWebRequest<TWebResponse> request, string hmacSecret, CancellationToken cancellationToken)
        where TQueuedMessage : IQueuedMessage
        where TWebResponse : IWebResponse, new()
    {
        var messageType = typeof(TQueuedMessage).FullName!;
        var messageId = message.MessageId!; // we expect all messages pulled from queue to be assigned an ID
        var caller = Caller.CreateAsMaintenanceTenant(message.CallId, message.TenantId);

        try
        {
            var posted = await serviceClient.PostAsync(caller, request, req =>
            {
                req.SetHMACAuth(request, hmacSecret);
                req.SetRequestId(caller.ToCall());
            }, cancellationToken);
            if (!posted.IsSuccessful)
            {
                throw posted.Error.ToException();
            }
        }
        catch (Exception ex)
        {
            recorder.Crash(caller.ToCall(), CrashLevel.NonCritical, ex,
                "Queued message {Id} of type {Type} failed delivery to API", messageId, messageType);
            throw;
        }

        recorder.TraceInformation(caller.ToCall(), "Relayed message {Id} of type {Type} to API", messageId,
            messageType);
    }
}