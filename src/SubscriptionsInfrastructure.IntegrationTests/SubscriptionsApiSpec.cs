#region

using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionsDomain;
using Xunit;

#endregion

namespace SubscriptionsInfrastructure.IntegrationTests;

[UsedImplicitly]
public class SubscriptionsApiSpec
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
        public async Task WhenGetSubscription_ThenReturns()
        {
            var login = await LoginUserAsync();

            var result = (await Api.GetAsync(new GetSubscriptionRequest
            {
                Id = login.DefaultOrganizationId!
            }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Subscription!;

            result.BuyerId.Should().Be(login.Profile!.UserId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.OwningEntityId.Should().Be(login.DefaultOrganizationId);
            result.ProviderState.Should().NotBeEmpty();
        }

        [Fact]
        public async Task WhenUpgradePlanByBuyer_ThenUpgradesToSamePlan()
        {
            var login = await LoginUserAsync();
            var organizationId = login.DefaultOrganizationId!;

            var result = (await Api.PutAsync(new ChangeSubscriptionPlanRequest
            {
                Id = organizationId,
                PlanId = SinglePlanBillingStateInterpreter.Constants.DefaultPlanId
            }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Subscription!;

            result.BuyerId.Should().Be(login.User.Id);
            result.OwningEntityId.Should().Be(organizationId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.Status.Should().Be(SubscriptionStatus.Activated);
            result.CanceledDateUtc.Should().BeNull();
            result.Plan.Id.Should().Be(SinglePlanBillingStateInterpreter.Constants.DefaultPlanId);
            result.Plan.IsTrial.Should().BeFalse();
            result.Plan.TrialEndDateUtc.Should().BeNull();
            result.Plan.Tier.Should().Be(SubscriptionTier.Standard);
            result.Period.Frequency.Should().Be(0);
            result.Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Invoice.Amount.Should().Be(0);
            result.Invoice.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Invoice.NextUtc.Should().BeNull();
        }

        [Fact]
        public async Task WhenUpgradePlanByOtherBillingAdminAndNotCanceledNorUnsubscribed_ThenBadRequest()
        {
            var loginA = await LoginUserAsync();
            var (login, organizationId) = await SetupOrganization(loginA);
            loginA = login;
            var loginB = await LoginUserAsync(LoginUser.PersonB);
            await SetupBillingAdminAsync(loginA, loginB, organizationId);
            loginB = await ReAuthenticateUserAsync(loginB);

            var result = await Api.PutAsync(new ChangeSubscriptionPlanRequest
            {
                Id = organizationId,
                PlanId = SinglePlanBillingStateInterpreter.Constants.DefaultPlanId
            }, req => req.SetJWTBearerToken(loginB.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Detail.Should().Be(Resources.SubscriptionRoot_ChangePlan_NotClaimable);
        }

        [Fact]
        public async Task WhenUpgradePlanByOtherBillingAdminAndUnsubscribed_ThenTransfers()
        {
            var loginA = await LoginUserAsync();
            var (login, organizationId) = await SetupOrganization(loginA);
            loginA = login;
            var loginB = await LoginUserAsync(LoginUser.PersonB);
            await SetupBillingAdminAsync(loginA, loginB, organizationId);
            loginB = await ReAuthenticateUserAsync(loginB);

            var @operator = await LoginUserAsync(LoginUser.Operator);
            await Api.DeleteAsync(new ForceCancelSubscriptionRequest
            {
                Id = organizationId
            }, req => req.SetJWTBearerToken(@operator.AccessToken));

            await PropagateDomainEventsAsync();
            var result = (await Api.PutAsync(new ChangeSubscriptionPlanRequest
            {
                Id = organizationId,
                PlanId = SinglePlanBillingStateInterpreter.Constants.DefaultPlanId
            }, req => req.SetJWTBearerToken(loginB.AccessToken))).Content.Value.Subscription!;

            result.BuyerId.Should().Be(loginB.User.Id);
            result.OwningEntityId.Should().Be(organizationId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.Status.Should().Be(SubscriptionStatus.Activated);
            result.CanceledDateUtc.Should().BeNull();
            result.Plan.Id.Should().Be(SinglePlanBillingStateInterpreter.Constants.DefaultPlanId);
            result.Plan.IsTrial.Should().BeFalse();
            result.Plan.TrialEndDateUtc.Should().BeNull();
            result.Plan.Tier.Should().Be(SubscriptionTier.Standard);
            result.Period.Frequency.Should().Be(0);
            result.Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Invoice.Amount.Should().Be(0);
            result.Invoice.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Invoice.NextUtc.Should().BeNull();
        }

        [Fact]
        public async Task WhenCancel_ThenUnsubscribes()
        {
            var loginA = await LoginUserAsync();
            var (login, organizationId) = await SetupOrganization(loginA);
            loginA = login;

            var result = (await Api.DeleteAsync(new CancelSubscriptionRequest
            {
                Id = organizationId
            }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Subscription!;

            result.OwningEntityId.Should().Be(organizationId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.Status.Should().Be(SubscriptionStatus.Unsubscribed);
            result.CanceledDateUtc.Should().BeNull();
            result.Plan.Id.Should().Be(string.Empty);
            result.Plan.IsTrial.Should().BeFalse();
            result.Plan.TrialEndDateUtc.Should().BeNull();
            result.Plan.Tier.Should().Be(SubscriptionTier.Unsubscribed);
            result.Period.Frequency.Should().Be(0);
            result.Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Invoice.Amount.Should().Be(0);
            result.Invoice.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Invoice.NextUtc.Should().BeNull();
        }

        [Fact]
        public async Task WhenForceCancel_ThenUnsubscribes()
        {
            var loginA = await LoginUserAsync();
            var (_, organizationId) = await SetupOrganization(loginA);

            var @operator = await LoginUserAsync(LoginUser.Operator);
            var result = (await Api.DeleteAsync(new ForceCancelSubscriptionRequest
            {
                Id = organizationId
            }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Subscription!;

            result.OwningEntityId.Should().Be(organizationId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.Status.Should().Be(SubscriptionStatus.Unsubscribed);
            result.CanceledDateUtc.Should().BeNull();
            result.Plan.Id.Should().Be(string.Empty);
            result.Plan.IsTrial.Should().BeFalse();
            result.Plan.TrialEndDateUtc.Should().BeNull();
            result.Plan.Tier.Should().Be(SubscriptionTier.Unsubscribed);
            result.Period.Frequency.Should().Be(0);
            result.Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Invoice.Amount.Should().Be(0);
            result.Invoice.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Invoice.NextUtc.Should().BeNull();
        }

        [Fact]
        public async Task WhenListBillingHistory_ThenReturnsHistory()
        {
            var loginA = await LoginUserAsync();
            var (login, organizationId) = await SetupOrganization(loginA);
            loginA = login;

            var fromUtc = new DateTime(2024, 06, 01, 0, 0, 0, DateTimeKind.Utc);
            var result = (await Api.GetAsync(new SearchSubscriptionHistoryRequest
            {
                Id = organizationId,
                FromUtc = fromUtc,
                ToUtc = null
            }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Invoices!;

            result.Count.Should().Be(4);
            result[0].InvoicedOnUtc.Should().Be(new DateTime(2024, 06, 01, 0, 0, 0, DateTimeKind.Utc));
            result[0].Amount.Should().Be(0);
            result[1].InvoicedOnUtc.Should().Be(new DateTime(2024, 07, 01, 0, 0, 0, DateTimeKind.Utc));
            result[1].Amount.Should().Be(0);
            result[2].InvoicedOnUtc.Should().Be(new DateTime(2024, 08, 01, 0, 0, 0, DateTimeKind.Utc));
            result[2].Amount.Should().Be(0);
            result[3].InvoicedOnUtc.Should().Be(new DateTime(2024, 09, 01, 0, 0, 0, DateTimeKind.Utc));
            result[3].Amount.Should().Be(0);
        }

        [Fact]
        public async Task WhenTransferSubscriptionToOtherBillingAdmin_ThenTransfers()
        {
            var loginA = await LoginUserAsync();
            var (login, organizationId) = await SetupOrganization(loginA);
            loginA = login;
            var loginB = await LoginUserAsync(LoginUser.PersonB);
            await SetupBillingAdminAsync(loginA, loginB, organizationId);
            loginB = await ReAuthenticateUserAsync(loginB);

            var result = (await Api.PutAsync(new TransferSubscriptionRequest
            {
                Id = organizationId,
                UserId = loginB.User.Id
            }, req => req.SetJWTBearerToken(loginA.AccessToken))).Content.Value.Subscription!;

            result.BuyerId.Should().Be(loginB.User.Id);
            result.OwningEntityId.Should().Be(organizationId);
            result.ProviderName.Should().Be(SinglePlanBillingStateInterpreter.Constants.ProviderName);
            result.Status.Should().Be(SubscriptionStatus.Activated);
            result.CanceledDateUtc.Should().BeNull();
            result.Plan.Id.Should().Be(SinglePlanBillingStateInterpreter.Constants.DefaultPlanId);
            result.Plan.IsTrial.Should().BeFalse();
            result.Plan.TrialEndDateUtc.Should().BeNull();
            result.Plan.Tier.Should().Be(SubscriptionTier.Standard);
            result.Period.Frequency.Should().Be(0);
            result.Period.Unit.Should().Be(PeriodFrequencyUnit.Eternity);
            result.Invoice.Amount.Should().Be(0);
            result.Invoice.Currency.Should().Be(CurrencyCodes.Default.Code);
            result.Invoice.NextUtc.Should().BeNull();
        }

        private async Task SetupBillingAdminAsync(LoginDetails loginA, LoginDetails loginB, string organizationId)
        {
            await Api.PostAsync(new InviteMemberToOrganizationRequest
            {
                Id = organizationId,
                UserId = loginB.User.Id
            }, req => req.SetJWTBearerToken(loginA.AccessToken));

            await PropagateDomainEventsAsync();
            await Api.PutAsync(new AssignRolesToOrganizationRequest
            {
                Id = organizationId,
                UserId = loginB.User.Id,
                Roles = [TenantRoles.BillingAdmin.Name]
            }, req => req.SetJWTBearerToken(loginA.AccessToken));
            await PropagateDomainEventsAsync();
        }

        private async Task<(LoginDetails login, string OrganizationId)> SetupOrganization(LoginDetails login)
        {
            var organization = await Api.PostAsync(new CreateOrganizationRequest
            {
                Name = "aname"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            var organizationId = organization.Content.Value.Organization!.Id;
            login = await ReAuthenticateUserAsync(login);

            return (login, organizationId);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IBillingProvider, SimpleBillingProvider>();
        }
    }
}