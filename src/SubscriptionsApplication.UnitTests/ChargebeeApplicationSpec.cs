using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class ChargebeeApplicationSpec
{
    private readonly ChargebeeApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<ISubscriptionsApplication> _subscriptionsApplication;
    private readonly Mock<IWebhookNotificationAuditService> _webhookNotificationAuditService;

    public ChargebeeApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _subscriptionsApplication = new Mock<ISubscriptionsApplication>();
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForBuyerAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname1", "avalue1" }
            });
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname1", "avalue1" }
            });
        _webhookNotificationAuditService = new Mock<IWebhookNotificationAuditService>();
        _webhookNotificationAuditService.Setup(wns => wns.CreateAuditAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookNotificationAudit
            {
                Id = "anauditid",
                Source = "asource",
                EventId = "aneventid",
                EventType = "aneventtype",
                Status = WebhookNotificationStatus.Received
            });
        _webhookNotificationAuditService.Setup(wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookNotificationAudit
            {
                Id = "anauditid",
                Source = "asource",
                EventId = "aneventid",
                EventType = "aneventtype",
                Status = WebhookNotificationStatus.Processed
            });

        _application = new ChargebeeApplication(recorder.Object, _subscriptionsApplication.Object,
            _webhookNotificationAuditService.Object);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithUnhandledEvent_ThenReturnsOk()
    {
        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.Unknown.ToString(), new ChargebeeEventContent(), CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerPaymentMethodChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionCancelledAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionPlanChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionDeletedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(wns => wns.CreateAuditAsync(_caller.Object,
            ChargebeeConstants.AuditSourceName,
            "aneventid", ChargebeeEventType.Unknown.ToString(), "{}", It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsFailedProcessingAsync(_caller.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithPaymentMethodChangedAndNoSubscriptionState_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid"
            }
        };
        _subscriptionsApplication.Setup(sa =>
                sa.GetProviderStateForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.PaymentSourceUpdated.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForBuyerAsync(_caller.Object, "acustomerid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerPaymentMethodChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithPaymentMethodChangedAndStateNoDifferent_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid",
                PaymentMethod = new ChargebeePaymentMethod
                {
                    Id = "apaymentmethodid",
                    Status = "apaymentstatus",
                    Type = "apaymenttype"
                }
            }
        };
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForBuyerAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                { ChargebeeConstants.MetadataProperties.PaymentMethodStatus, "apaymentstatus" },
                { ChargebeeConstants.MetadataProperties.PaymentMethodType, "apaymenttype" }
            });

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.PaymentSourceUpdated.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForBuyerAsync(_caller.Object, "acustomerid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerPaymentMethodChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithPaymentMethodChangedAndStateIsDifferent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid",
                PaymentMethod = new ChargebeePaymentMethod
                {
                    Id = "apaymentmethodid",
                    Status = "apaymentstatus",
                    Type = "apaymenttype"
                }
            }
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.PaymentSourceUpdated.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForBuyerAsync(_caller.Object, "acustomerid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerPaymentMethodChangedAsync(_caller.Object,
                ChargebeeConstants.ProviderName,
                It.Is<SubscriptionMetadata>(metadata =>
                    metadata.Count == 4
                    && metadata["aname1"] == "avalue1"
                    && metadata[ChargebeeConstants.MetadataProperties.CustomerId] == "acustomerid"
                    && metadata[ChargebeeConstants.MetadataProperties.PaymentMethodStatus] == "apaymentstatus"
                    && metadata[ChargebeeConstants.MetadataProperties.PaymentMethodType] == "apaymenttype"
                ), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(wns =>
            wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithCustomerDeletedAndNoSubscriptionState_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid"
            }
        };
        _subscriptionsApplication.Setup(sa =>
                sa.GetProviderStateForBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.CustomerDeleted.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForBuyerAsync(_caller.Object, "acustomerid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerDeletedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithCustomerDeletedAndStateIsDifferent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid",
                PaymentMethod = new ChargebeePaymentMethod
                {
                    Id = "apaymentmethodid",
                    Status = "apaymentstatus",
                    Type = "apaymenttype"
                }
            }
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.CustomerDeleted.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForBuyerAsync(_caller.Object, "acustomerid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifyBuyerDeletedAsync(_caller.Object,
                ChargebeeConstants.ProviderName,
                It.Is<SubscriptionMetadata>(metadata =>
                    metadata.Count == 3
                    && metadata[ChargebeeConstants.MetadataProperties.CustomerId] == "acustomerid"
                    && metadata[ChargebeeConstants.MetadataProperties.PaymentMethodStatus] == "apaymentstatus"
                    && metadata[ChargebeeConstants.MetadataProperties.PaymentMethodType] == "apaymenttype"
                ), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(wns =>
            wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionCancelledAndNoSubscriptionState_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid"
            }
        };
        _subscriptionsApplication.Setup(sa =>
                sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionCancelled.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionCancelledAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionCancelledAndStateNoDifferent_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                { ChargebeeConstants.MetadataProperties.SubscriptionStatus, "asubscriptionstatus" },
                { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False" },
                { ChargebeeConstants.MetadataProperties.CanceledAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingAmount, "1.1" },
                { ChargebeeConstants.MetadataProperties.CurrencyCode, "acurrencycode" },
                { ChargebeeConstants.MetadataProperties.NextBillingAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1" },
                { ChargebeeConstants.MetadataProperties.BillingPeriodUnit, "abillingperiodunit" },
                { ChargebeeConstants.MetadataProperties.PlanId, "anitempriceid" },
                { ChargebeeConstants.MetadataProperties.TrialEnd, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() }
            });

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionCancelled.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionCancelledAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionCancelledAndStateIsDifferent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionCancelled.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionCancelledAsync(_caller.Object, ChargebeeConstants.ProviderName,
                It.Is<SubscriptionMetadata>(metadata =>
                    metadata.Count == 13
                    && metadata["aname1"] == "avalue1"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionId] == "asubscriptionid"
                    && metadata[ChargebeeConstants.MetadataProperties.CustomerId] == "acustomerid"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionStatus] == "asubscriptionstatus"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionDeleted] == "False"
                    && metadata[ChargebeeConstants.MetadataProperties.CanceledAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingAmount] == "1.1"
                    && metadata[ChargebeeConstants.MetadataProperties.CurrencyCode] == "acurrencycode"
                    && metadata[ChargebeeConstants.MetadataProperties.NextBillingAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodValue] == "1"
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodUnit] == "abillingperiodunit"
                    && metadata[ChargebeeConstants.MetadataProperties.PlanId] == "anitempriceid"
                    && metadata[ChargebeeConstants.MetadataProperties.TrialEnd]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                ), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(wns =>
            wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionPlanChangedAndNoSubscriptionState_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid"
            }
        };
        _subscriptionsApplication.Setup(sa =>
                sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionChanged.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionPlanChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionPlanChangedAndStateNoDifferent_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                { ChargebeeConstants.MetadataProperties.SubscriptionStatus, "asubscriptionstatus" },
                { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False" },
                { ChargebeeConstants.MetadataProperties.CanceledAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingAmount, "1.1" },
                { ChargebeeConstants.MetadataProperties.CurrencyCode, "acurrencycode" },
                { ChargebeeConstants.MetadataProperties.NextBillingAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1" },
                { ChargebeeConstants.MetadataProperties.BillingPeriodUnit, "abillingperiodunit" },
                { ChargebeeConstants.MetadataProperties.PlanId, "anitempriceid" },
                { ChargebeeConstants.MetadataProperties.TrialEnd, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() }
            });

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionChanged.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionPlanChangedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionPlanChangedAndStateIsDifferent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionChanged.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionPlanChangedAsync(_caller.Object,
                ChargebeeConstants.ProviderName,
                It.Is<SubscriptionMetadata>(metadata =>
                    metadata.Count == 13
                    && metadata["aname1"] == "avalue1"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionId] == "asubscriptionid"
                    && metadata[ChargebeeConstants.MetadataProperties.CustomerId] == "acustomerid"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionStatus] == "asubscriptionstatus"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionDeleted] == "False"
                    && metadata[ChargebeeConstants.MetadataProperties.CanceledAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingAmount] == "1.1"
                    && metadata[ChargebeeConstants.MetadataProperties.CurrencyCode] == "acurrencycode"
                    && metadata[ChargebeeConstants.MetadataProperties.NextBillingAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodValue] == "1"
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodUnit] == "abillingperiodunit"
                    && metadata[ChargebeeConstants.MetadataProperties.PlanId] == "anitempriceid"
                    && metadata[ChargebeeConstants.MetadataProperties.TrialEnd]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                ), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(wns =>
            wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionDeletedAndNoSubscriptionState_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid"
            }
        };
        _subscriptionsApplication.Setup(sa =>
                sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionDeleted.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionDeletedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionDeletedAndStateNoDifferent_ThenDoesNothing()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };
        _subscriptionsApplication.Setup(sa => sa.GetProviderStateForSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { ChargebeeConstants.MetadataProperties.SubscriptionId, "asubscriptionid" },
                { ChargebeeConstants.MetadataProperties.CustomerId, "acustomerid" },
                { ChargebeeConstants.MetadataProperties.SubscriptionStatus, "asubscriptionstatus" },
                { ChargebeeConstants.MetadataProperties.SubscriptionDeleted, "False" },
                { ChargebeeConstants.MetadataProperties.CanceledAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingAmount, "1.1" },
                { ChargebeeConstants.MetadataProperties.CurrencyCode, "acurrencycode" },
                { ChargebeeConstants.MetadataProperties.NextBillingAt, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() },
                { ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1" },
                { ChargebeeConstants.MetadataProperties.BillingPeriodUnit, "abillingperiodunit" },
                { ChargebeeConstants.MetadataProperties.PlanId, "anitempriceid" },
                { ChargebeeConstants.MetadataProperties.TrialEnd, DateTime.UnixEpoch.AddSeconds(1).ToIso8601() }
            });

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionDeleted.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionDeletedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SubscriptionMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _webhookNotificationAuditService.Verify(
            wns => wns.MarkAsProcessedAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNotifyWebhookEventWithSubscriptionDeletedAndStateIsDifferent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Subscription = new ChargebeeEventSubscription
            {
                Id = "asubscriptionid",
                CustomerId = "acustomerid",
                Status = "asubscriptionstatus",
                Deleted = false,
                CancelledAt = 1,
                SubscriptionItems =
                [
                    new ChargebeeEventSubscriptionItem
                    {
                        Amount = 1.1M,
                        ItemPriceId = "anitempriceid"
                    }
                ],
                CurrencyCode = "acurrencycode",
                NextBillingAt = 1,
                BillingPeriod = 1,
                BillingPeriodUnit = "abillingperiodunit",
                TrialEnd = 1
            }
        };

        var result = await _application.NotifyWebhookEvent(_caller.Object, "aneventid",
            ChargebeeEventType.SubscriptionDeleted.ToString(), content, CancellationToken.None);

        result.Should().BeSuccess();
        _subscriptionsApplication.Verify(sa =>
            sa.GetProviderStateForSubscriptionAsync(_caller.Object, "asubscriptionid", It.IsAny<CancellationToken>()));
        _subscriptionsApplication.Verify(
            sa => sa.NotifySubscriptionDeletedAsync(_caller.Object, ChargebeeConstants.ProviderName,
                It.Is<SubscriptionMetadata>(metadata =>
                    metadata.Count == 13
                    && metadata["aname1"] == "avalue1"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionId] == "asubscriptionid"
                    && metadata[ChargebeeConstants.MetadataProperties.CustomerId] == "acustomerid"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionStatus] == "asubscriptionstatus"
                    && metadata[ChargebeeConstants.MetadataProperties.SubscriptionDeleted] == "False"
                    && metadata[ChargebeeConstants.MetadataProperties.CanceledAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingAmount] == "1.1"
                    && metadata[ChargebeeConstants.MetadataProperties.CurrencyCode] == "acurrencycode"
                    && metadata[ChargebeeConstants.MetadataProperties.NextBillingAt]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodValue] == "1"
                    && metadata[ChargebeeConstants.MetadataProperties.BillingPeriodUnit] == "abillingperiodunit"
                    && metadata[ChargebeeConstants.MetadataProperties.PlanId] == "anitempriceid"
                    && metadata[ChargebeeConstants.MetadataProperties.TrialEnd]
                    == DateTime.UnixEpoch.AddSeconds(1).ToIso8601()
                ), It.IsAny<CancellationToken>()));
        _webhookNotificationAuditService.Verify(wns =>
            wns.MarkAsProcessedAsync(_caller.Object, "anauditid", It.IsAny<CancellationToken>()));
    }
}