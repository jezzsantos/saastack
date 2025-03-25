using Application.Resources.Shared;
using Infrastructure.External.ApplicationServices;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.External.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class InMemPricingPlansCacheSpec
{
    private readonly ChargebeeHttpServiceClient.InMemPricingPlansCache _cache;

    public InMemPricingPlansCacheSpec()
    {
        _cache = new ChargebeeHttpServiceClient.InMemPricingPlansCache(TimeSpan.Zero);
    }

    [Fact]
    public async Task WhenGetAsyncAndNotCached_ThenReturnsNone()
    {
        var result = await _cache.GetAsync(CancellationToken.None);

        result.Should().BeNone();
    }

    [Fact]
    public async Task WhenGetAsyncAndCachedButExpired_ThenReturnsNone()
    {
        var plans = new PricingPlans();
        await _cache.SetAsync(plans, CancellationToken.None);
        var cache = new ChargebeeHttpServiceClient.InMemPricingPlansCache(TimeSpan.Zero);

        var result = await cache.GetAsync(CancellationToken.None);

        result.Should().BeNone();
    }

    [Fact]
    public async Task WhenGetAsyncAndCachedAndNotExpired_ThenReturnsPlans()
    {
        var plans = new PricingPlans();
        await _cache.SetAsync(plans, CancellationToken.None);
        var cache = new ChargebeeHttpServiceClient.InMemPricingPlansCache(TimeSpan.FromMinutes(1));

        var result = await cache.GetAsync(CancellationToken.None);

        result.Should().BeNone();
    }
}