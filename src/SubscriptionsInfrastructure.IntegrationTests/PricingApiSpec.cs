using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using FluentAssertions;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionsInfrastructure.IntegrationTests.Stubs;
using Xunit;

namespace SubscriptionsInfrastructure.IntegrationTests;

[UsedImplicitly]
public class PricingApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenSimpleBillingProvider : WebApiSpec<Program>
    {
        public GivenSimpleBillingProvider(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenListPricingPlans_ThenReturnsPlans()
        {
            var result = (await Api.GetAsync(new ListPricingPlansRequest())).Content.Value.Plans;

            result.Eternally.Count.Should().Be(1);
            result.Eternally[0].Id.Should().Be(SinglePlanBillingStateInterpreter.Constants.DefaultPlanId);
            result.Eternally[0].Cost.Should().Be(0);
            result.Eternally[0].Period.Frequency.Should().Be(1);
            result.Eternally[0].Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Eternally[0].Trial!.HasTrial.Should().BeFalse();
            result.Eternally[0].FeatureSection.Count.Should().Be(1);
            result.Eternally[0].FeatureSection[0].Features.Count.Should().Be(1);
            result.Eternally[0].FeatureSection[0].Features[0].IsIncluded.Should().BeTrue();
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IBillingProvider, SimpleBillingProvider>();
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenChargebeeBillingProvider : WebApiSpec<Program>
    {
        public GivenChargebeeBillingProvider(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenListPricingPlans_ThenReturnsPlans()
        {
            var result = (await Api.GetAsync(new ListPricingPlansRequest())).Content.Value.Plans;

            result.Eternally.Count.Should().Be(0);
            result.Monthly.Count.Should().Be(0);
            result.Weekly.Count.Should().Be(0);
            result.Daily.Count.Should().Be(0);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddPerHttpRequest<IBillingProvider, StubChargebeeBillingProvider>();
        }
    }
}