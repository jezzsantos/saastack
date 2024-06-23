using System.Net;
using ApiHost1;
using Application.Services.Shared;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionsDomain;
using Xunit;

namespace SubscriptionsInfrastructure.IntegrationTests;

[UsedImplicitly]
public class MigrationsApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenBasicBillingProvider : WebApiSpec<Program>
    {
        public GivenBasicBillingProvider(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenExportSubscriptionsToBeMigrated_ThenReturnsSubscriptions()
        {
            var login = await LoginUserAsync();

            var request = new ExportSubscriptionsToMigrateRequest();
            var result = (await Api.GetAsync(request,
                    req => req.SetHMACAuth(request, "asecret")))
                .Content.Value.Subscriptions!;

            result.Count.Should().Be(1);
            result[0].BuyerId.Should().Be(login.Profile!.UserId);
            result[0].OwningEntityId.Should().Be(login.DefaultOrganizationId);
            result[0].ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result[0].ProviderState.Should().NotBeEmpty();
            result[0].Buyer[nameof(SubscriptionBuyer.Id)].Should().Be(login.Profile!.UserId);
            result[0].Buyer[nameof(SubscriptionBuyer.Name)].Should()
                .Be("{\"FirstName\":\"persona\",\"LastName\":\"alastname\"}");
            result[0].Buyer[nameof(SubscriptionBuyer.EmailAddress)].Should().Be(login.Profile!.EmailAddress);
            result[0].Buyer[nameof(SubscriptionBuyer.CompanyReference)].Should().Be(login.DefaultOrganizationId);
            result[0].Buyer[nameof(SubscriptionBuyer.Address)].Should().Be(
                "{\"City\":\"\",\"CountryCode\":\"NZL\",\"Line1\":\"\",\"Line2\":\"\",\"Line3\":\"\",\"State\":\"\",\"Zip\":\"\"}");
        }

        [Fact]
        public async Task WhenMigrateSubscriptionWithInstalledProvider_ThenReturnsError()
        {
            var login = await LoginUserAsync();

            var organizationId = login.DefaultOrganizationId!;
            var request = new MigrateSubscriptionRequest
            {
                Id = organizationId,
                ProviderName = "anunknownprovider",
                ProviderState = new Dictionary<string, string>
                {
                    { "aname", "avalue" }
                }
            };

            var result =
                await Api.PutAsync(request,
                    req => req.SetHMACAuth(request, "asecret"));

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Detail.Should().Be(Resources.SubscriptionRoot_ProviderMismatch);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IBillingProvider, SimpleBillingProvider>();
        }
    }
}