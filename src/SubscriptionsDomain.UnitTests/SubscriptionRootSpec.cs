using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionRootSpec
{
    private readonly Mock<IBillingStateInterpreter> _interpreter;
    private readonly SubscriptionRoot _subscription;

    public SubscriptionRootSpec()
    {
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var recorder = new Mock<IRecorder>();
        _interpreter = new Mock<IBillingStateInterpreter>();
        _interpreter.Setup(p => p.ProviderName)
            .Returns("aprovidername");
        _interpreter.Setup(p => p.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _interpreter.Setup(p => p.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        _interpreter.Setup(sp => sp.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);

        _subscription = SubscriptionRoot.Create(recorder.Object, identifierFactory.Object, "anowningentityid".ToId(),
            "abuyerid".ToId(), _interpreter.Object).Value;
    }

    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.OwningEntityId.Should().Be("anowningentityid".ToId());
        Enumerable.Last(_subscription.Events).Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenEnsureInvariantsAndBuyerIdIsEmpty_ThenReturnsErrors()
    {
#if TESTINGONLY
        _subscription.TestingOnly_SetDetails(Identifier.Empty(), _subscription.OwningEntityId);
#endif

        var result = _subscription.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoBuyer);
    }

    [Fact]
    public void WhenEnsureInvariantsAndOwningEntityIdIsEmpty_ThenReturnsErrors()
    {
#if TESTINGONLY
        _subscription.TestingOnly_SetDetails(_subscription.BuyerId, Identifier.Empty());
#endif

        var result = _subscription.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoOwningEntity);
    }

    [Fact]
    public void WhenSetProviderByAnotherUser_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _subscription.SetProvider(provider, "anotheruserid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.SubscriptionRoot_NotBuyer);
    }

    [Fact]
    public void WhenSetProviderAndAlreadyInitialized_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        _subscription.ChangeProvider(provider, CallerConstants.MaintenanceAccountUserId.ToId(), _interpreter.Object);

        var result = _subscription.SetProvider(provider, "abuyerid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_SameProvider);
    }

    [Fact]
    public void WhenSetProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result = _subscription.SetProvider(provider, "abuyerid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenSetProvider_ThenSets()
    {
        var metadata = new SubscriptionMetadata { { "aname", "avalue" } };
        var provider = BillingProvider.Create("aprovidername", metadata).Value;

        _subscription.SetProvider(provider, "abuyerid".ToId(),
            _interpreter.Object);

        _subscription.Provider.Value.Name.Should().Be("aprovidername");
        _subscription.Provider.Value.State.Should().BeEquivalentTo(metadata);
        Enumerable.Last(_subscription.Events).Should().BeOfType<ProviderChanged>();
        _interpreter.Verify(sp => sp.SetInitialProviderState(provider));
    }

    [Fact]
    public void WhenChangeProviderByAnyUser_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = _subscription.ChangeProvider(provider, "auserid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.SubscriptionRoot_ChangeProvider_NotAuthorized);
    }

    [Fact]
    public void WhenChangeProviderAndAlreadyInitialized_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        _subscription.ChangeProvider(provider, CallerConstants.MaintenanceAccountUserId.ToId(), _interpreter.Object);

        var result = _subscription.ChangeProvider(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_SameProvider);
    }

    [Fact]
    public void WhenChangeProviderAndNotSameAsInstalledProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result = _subscription.ChangeProvider(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenChangeBillingProvider_ThenChanged()
    {
        var metadata = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        };
        var provider = BillingProvider.Create("aprovidername", metadata).Value;

        _subscription.ChangeProvider(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object);

        _subscription.Provider.Value.Name.Should().Be("aprovidername");
        _subscription.Provider.Value.State.Should().BeEquivalentTo(metadata);
        Enumerable.Last(_subscription.Events).Should().BeOfType<ProviderChanged>();
        _interpreter.Verify(sp => sp.SetInitialProviderState(provider), Times.Never);
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ViewSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByBuyer_ThenReturnsSubscription()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        var providerSubscription = ProviderSubscription.Empty;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(providerSubscription);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            (_, _) => Task.FromResult(Permission.Allowed));

        result.Should().BeSuccess();
        result.Value.Should().Be(providerSubscription);
        _interpreter.Verify(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByServiceAccount_ThenReturnsSubscription()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        var providerSubscription = ProviderSubscription.Empty;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(providerSubscription);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object,
            CallerConstants.MaintenanceAccountUserId.ToId(),
            (_, _) => Task.FromResult(Permission.Allowed));

        result.Should().BeSuccess();
        result.Value.Should().Be(providerSubscription);
        _interpreter.Verify(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenChangePlanAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "amodifierid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePlanAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "amodifierid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_ChangePlan_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.FeatureViolation, Resources.SubscriptionRoot_ChangePlan_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerWithPaymentMethod_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByWebhookWithPaymentMethod_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.FeatureViolation,
            Resources.SubscriptionRoot_TransferSubscription_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherButSubscriptionIsNotCanceled_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ChangePlan_NotClaimable);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherAndCanceled_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("auserid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherAndUnsubscribed_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Unsubscribed, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("auserid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "acancellerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "acancellerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_CancelSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyerButNotCancellable_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.SubscriptionRoot_CancelSubscription_NotCancellable);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByOperations_ThenCanceled()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "anotheruserid".ToId(),
            Roles.Create(PlatformRoles.Operations).Value,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }), false);

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyer_ThenCanceled()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }), false);

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "atransfererid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "atransfererid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByAnother_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "auserid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_NotBuyer);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_TransferSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.FeatureViolation,
            Resources.SubscriptionRoot_TransferSubscription_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerAndActivated_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("atransfereeid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerAndCanceled_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value));
        _interpreter.Setup(sp => sp.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(sp => sp.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("atransfereeid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
        _interpreter.Verify(sp => sp.GetBuyerReference(provider));
        _interpreter.Verify(sp => sp.GetSubscriptionReference(provider));
    }

    [Fact]
    public void WhenDeleteSubscriptionWithWrongOwningEntityId_ThenReturnsError()
    {
        var result = _subscription.DeleteSubscription("adeleterid".ToId(), "anotherentityid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_DeleteSubscription_NotOwningEntityId);
    }

    [Fact]
    public void WhenDeleteSubscription_ThenDeletes()
    {
        var result = _subscription.DeleteSubscription("adeleterid".ToId(), "anowningentityid".ToId());

        result.Should().BeSuccess();
        _subscription.Events.Last().Should().BeOfType<Deleted>();
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_UnsubscribeSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncButCannotBeUnsubscribed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false)
                    .Value).Value);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_CannotBeUnsubscribed);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsync_ThenUnsubscribes()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderSubscriptionReference.Should().BeNone();
        _subscription.Events.Last().Should().BeOfType<SubscriptionUnsubscribed>();
        _interpreter.Verify(sp => sp.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByAnother_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeBuyerPaymentMethodFromProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByBuyer_ThenChanges()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "abuyerid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByServiceAccount_ThenChanges()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object,
            CallerConstants.MaintenanceAccountUserId.ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public void WhenChangePaymentMethodForBuyerFromProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.ChangePaymentMethodForBuyerFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenChangePaymentMethodForBuyerFromProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangePaymentMethodForBuyerFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public void WhenChangePaymentMethodForBuyerFromProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangePaymentMethodForBuyerFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeBuyerPaymentMethodFromProvider_NotAuthorized);
    }

    [Fact]
    public void WhenChangePaymentMethodForBuyerFromProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.ChangePaymentMethodForBuyerFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public void WhenChangePaymentMethodForBuyerFromProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = _subscription.ChangePaymentMethodForBuyerFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public void WhenCancelSubscriptionFromProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.CancelSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenCancelSubscriptionFromProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.CancelSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public void WhenCancelSubscriptionFromProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.CancelSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_CancelSubscriptionFromProvider_NotAuthorized);
    }

    [Fact]
    public void WhenCancelSubscriptionFromProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.CancelSubscriptionFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public void WhenCancelSubscriptionFromProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = _subscription.CancelSubscriptionFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
    }

    [Fact]
    public void WhenChangeSubscriptionPlanFromProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.ChangeSubscriptionPlanFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenChangeSubscriptionPlanFromProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangeSubscriptionPlanFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public void WhenChangeSubscriptionPlanFromProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangeSubscriptionPlanFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeSubscriptionPlanFromProvider_NotAuthorized);
    }

    [Fact]
    public void WhenChangeSubscriptionPlanFromProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.ChangeSubscriptionPlanFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public void WhenChangeSubscriptionPlanFromProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(p => p.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionid".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty));

        var result = _subscription.ChangeSubscriptionPlanFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
    }

    [Fact]
    public void WhenDeleteSubscriptionFromProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.DeleteSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public void WhenDeleteSubscriptionFromProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.DeleteSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public void WhenDeleteSubscriptionFromProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.DeleteSubscriptionFromProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_DeleteSubscriptionFromProvider_NotAuthorized);
    }

    [Fact]
    public void WhenDeleteSubscriptionFromProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.DeleteSubscriptionFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public void WhenDeleteSubscriptionFromProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(p => p.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionid".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty));

        var result = _subscription.DeleteSubscriptionFromProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().BeNone();
        _subscription.Events.Last().Should().BeOfType<SubscriptionUnsubscribed>();
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedFromProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.RestoreBuyerAfterDeletedFromProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedFromProviderAsyncWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.RestoreBuyerAfterDeletedFromProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedFromProviderAsyncByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.RestoreBuyerAfterDeletedFromProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_RestoreBuyerAfterDeletedFromProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedFromProviderAsync_ThenRestoresBuyer()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = await _subscription.RestoreBuyerAfterDeletedFromProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname2", "avalue2" }
            }));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Last().Should().BeOfType<BuyerRestored>();
    }
}