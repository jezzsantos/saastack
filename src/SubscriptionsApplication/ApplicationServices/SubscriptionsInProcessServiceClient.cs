using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;

namespace SubscriptionsApplication.ApplicationServices;

public class SubscriptionsInProcessServiceClient : ISubscriptionsService
{
    private readonly Func<ISubscriptionsApplication> _subscriptionsApplicationFactory;
    private ISubscriptionsApplication? _application;

    /// <summary>
    ///     HACK: LazyGetRequiredService and <see cref="Func{TResult}" /> is needed here to avoid the runtime cyclic dependency
    ///     between
    ///     <see cref="ISubscriptionsApplication" /> requiring <see cref="ISubscriptionOwningEntityService" />, which requires
    ///     <see cref="ISubscriptionsService" />
    /// </summary>
    public SubscriptionsInProcessServiceClient(Func<ISubscriptionsApplication> subscriptionsApplicationFactory)
    {
        _subscriptionsApplicationFactory = subscriptionsApplicationFactory;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await GetApplication().GetSubscriptionPrivateAsync(caller, id, cancellationToken);
    }

    private ISubscriptionsApplication GetApplication()
    {
        if (_application.NotExists())
        {
            _application = _subscriptionsApplicationFactory();
        }

        return _application;
    }
}