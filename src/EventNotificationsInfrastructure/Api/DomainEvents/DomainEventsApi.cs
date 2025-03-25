using Application.Resources.Shared;
using Common;
using EventNotificationsApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;

namespace EventNotificationsInfrastructure.Api.DomainEvents;

public class DomainEventsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IDomainEventsApplication _domainEventsApplication;

    public DomainEventsApi(ICallerContextFactory callerFactory, IDomainEventsApplication domainEventsApplication)
    {
        _callerFactory = callerFactory;
        _domainEventsApplication = domainEventsApplication;
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllEventNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _domainEventsApplication.DrainAllDomainEventsAsync(_callerFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Notify(NotifyDomainEventRequest request,
        CancellationToken cancellationToken)
    {
        var notified =
            await _domainEventsApplication.NotifyDomainEventAsync(_callerFactory.Create(), request.SubscriptionName!,
                request.Message!,
                cancellationToken);

        return () => notified.HandleApplicationResult<bool, DeliverMessageResponse>(nt =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsSent = nt }));
    }

#if TESTINGONLY
    public async Task<ApiSearchResult<EventNotification, SearchAllEventNotificationsResponse>> SearchAll(
        SearchAllEventNotificationsRequest request, CancellationToken cancellationToken)
    {
        var events = await _domainEventsApplication.SearchAllNotificationsAsync(_callerFactory.Create(),
            request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () => events.HandleApplicationResult(evt => new SearchAllEventNotificationsResponse
            { Notifications = evt.Results, Metadata = evt.Metadata });
    }
#endif
}