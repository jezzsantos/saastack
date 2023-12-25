using Application.Common;
using Application.Interfaces.Services;
using Common.Recording;
using Domain.Interfaces.Services;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides a <see cref="IMetricReporter" /> that forwards the measurement to the backend Ancillary API
/// </summary>
public class ForwardToAncillaryApiMetrics : IMetricReporter
{
    private readonly string _hmacSecret;
    private readonly IServiceClient _serviceClient;

    public ForwardToAncillaryApiMetrics(IDependencyContainer container) : this(
        new InterHostServiceClient(container.Resolve<IHttpClientFactory>(),
            container.Resolve<IHostSettings>().GetAncillaryApiHostBaseUrl()),
        container.Resolve<IHostSettings>().GetAncillaryApiHostHmacAuthSecret())
    {
    }

    private ForwardToAncillaryApiMetrics(IServiceClient serviceClient, string hmacSecret)
    {
        _serviceClient = serviceClient;
        _hmacSecret = hmacSecret;
    }

    public void Measure(string eventName, Dictionary<string, object>? additional = null)
    {
        // TODO: If we are running on a BackEndForFrontEndWebHost we need to copy the bearer token from the cookie into the caller.Authorization
        var caller = Caller.CreateAsAnonymous();
        var request = new RecordMeasureRequest
        {
            EventName = eventName,
            Additional = additional!
        };
        _serviceClient.PostAsync(caller, request, req =>
        {
            req.SetHmacAuth(request, _hmacSecret);
            req.SetRequestId(caller.ToCall());
        }).GetAwaiter().GetResult();
    }
}