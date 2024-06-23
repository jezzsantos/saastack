using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class PricingApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public PricingApi(ICallerContextFactory callerFactory, ISubscriptionsApplication subscriptionsApplication)
    {
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<ApiGetResult<PricingPlans, ListPricingPlansResponse>> ListPricingPlans(
        ListPricingPlansRequest request, CancellationToken cancellationToken)
    {
        var plans = await _subscriptionsApplication.ListPricingPlansAsync(_callerFactory.Create(), cancellationToken);

        return () =>
            plans.HandleApplicationResult<PricingPlans, ListPricingPlansResponse>(p =>
                new ListPricingPlansResponse
                    { Plans = p });
    }
}