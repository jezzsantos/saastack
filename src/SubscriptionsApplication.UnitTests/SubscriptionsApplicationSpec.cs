using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using UnitTesting.Common;
using Xunit;
using Subscription = SubscriptionsApplication.Persistence.ReadModels.Subscription;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionsApplicationSpec
{
    private readonly SubscriptionsApplication _application;
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<ISubscriptionOwningEntityService> _owningEntityService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionRepository> _repository;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public SubscriptionsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId).Returns("acallerid");
        _userProfilesService = new Mock<IUserProfilesService>();
        _billingProvider = new Mock<IBillingProvider>();
        _billingProvider.Setup(bp => bp.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value).Value);
        _billingProvider.Setup(bp => bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);
        _owningEntityService = new Mock<ISubscriptionOwningEntityService>();
        _repository = new Mock<ISubscriptionRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, CancellationToken _) => root);

        _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
            _userProfilesService.Object, _billingProvider.Object, _owningEntityService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenGetSubscriptionAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.GetSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _owningEntityService.Verify(
            oes => oes.CanViewSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenGetSubscriptionAsync_ThenReturnsSubscription()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _owningEntityService.Setup(oes => oes.CanViewSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);
        var result =
            await _application.GetSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _owningEntityService.Verify(
            oes => oes.CanViewSubscriptionAsync(_caller.Object, "anowningentityid", "acallerid",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangePlanAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.ChangePlanAsync(_caller.Object, "anowningentityid", "aplanid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _owningEntityService.Verify(
            oes => oes.CanChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
            It.IsAny<ChangePlanOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyer_ThenChangesPlan()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _caller.Setup(cc => cc.CallerId)
            .Returns("abuyerid");
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
                It.IsAny<ChangePlanOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _owningEntityService.Setup(oes => oes.CanChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.ChangePlanAsync(_caller.Object, "anowningentityid", "aplanid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.ProviderBuyerReference == "abuyerreference"
            && root.ProviderSubscriptionReference == "asubscriptionreference"
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanChangeSubscriptionPlanAsync(_caller.Object, "anowningentityid", "abuyerid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
            It.Is<ChangePlanOptions>(options =>
                options.Subscriber.EntityId == "anowningentityid"
                && options.Subscriber.EntityType == "Organization"
                && options.PlanId == "aplanid"),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(
            bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByWebhook_ThenChangesPlan()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
                It.IsAny<ChangePlanOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _owningEntityService.Setup(oes => oes.CanChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.ChangePlanAsync(_caller.Object, "anowningentityid", "aplanid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.ProviderBuyerReference == "abuyerreference"
            && root.ProviderSubscriptionReference == "asubscriptionreference"
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
            It.Is<ChangePlanOptions>(options =>
                options.Subscriber.EntityId == "anowningentityid"
                && options.Subscriber.EntityType == "Organization"
                && options.PlanId == "aplanid"),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(
            bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherBillingAdmin_ThenTransfersPlan()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Address = new ProfileAddress
                {
                    CountryCode = "acountrycode"
                },
                Classification = UserProfileClassification.Person,
                DisplayName = "adisplayname",
                EmailAddress = "auser@company.com",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });
        _owningEntityService.Setup(oes => oes.CanChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.ChangePlanAsync(_caller.Object, "anowningentityid", "aplanid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("acallerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "acallerid".ToId()
            && root.ProviderBuyerReference == "abuyerreference"
            && root.ProviderSubscriptionReference == "asubscriptionreference"
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanChangeSubscriptionPlanAsync(_caller.Object, "anowningentityid", "acallerid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(_caller.Object, "acallerid", It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.ChangeSubscriptionPlanAsync(It.IsAny<ICallerContext>(),
            It.IsAny<ChangePlanOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(
            bp => bp.GatewayService.TransferSubscriptionAsync(_caller.Object,
                It.Is<TransferSubscriptionOptions>(options =>
                    options.TransfereeBuyer.Id == "acallerid"
                    && options.TransfereeBuyer.Name.FirstName == "afirstname"
                    && options.PlanId == "aplanid"
                    && options.TransfereeBuyer.Subscriber.EntityId == "anowningentityid"
                    && options.TransfereeBuyer.Subscriber.EntityType == "Organization"
                ),
                It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.CancelSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _owningEntityService.Verify(
            oes => oes.CanCancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<CancelSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyer_ThenCancelsSubscription()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _caller.Setup(cc => cc.CallerId)
            .Returns("abuyerid");
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles());
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value).Value);
        _billingProvider.Setup(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<CancelSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _owningEntityService.Setup(oes => oes.CanCancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.CancelSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanCancelSubscriptionAsync(_caller.Object, "anowningentityid", "abuyerid",
                It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
            It.Is<CancelSubscriptionOptions>(options =>
                options.CancelWhen == CancelSubscriptionSchedule.EndOfTerm),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenForceCancelSubscriptionAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.ForceCancelSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _owningEntityService.Verify(
            oes => oes.CanCancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<CancelSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenForceCancelSubscriptionAsyncByOperations_ThenCancelsSubscription()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _caller.Setup(cc => cc.CallerId)
            .Returns("abuyerid");
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles());
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None)
                    .Value).Value);
        _billingProvider.Setup(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<CancelSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _owningEntityService.Setup(oes => oes.CanCancelSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.ForceCancelSubscriptionAsync(_caller.Object, "anowningentityid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanCancelSubscriptionAsync(_caller.Object, "anowningentityid", "abuyerid",
                It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
            It.Is<CancelSubscriptionOptions>(options =>
                options.CancelWhen == CancelSubscriptionSchedule.Immediately),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenListPricingPlans_ThenReturnsPlans()
    {
        var plans = new PricingPlans();
        _billingProvider.Setup(bp =>
                bp.GatewayService.ListAllPricingPlansAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var result = await _application.ListPricingPlansAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().Be(plans);
    }

    [Fact]
    public async Task WhenSearchSubscriptionHistoryAsyncAndNoDates_ThenReturnsHistoryForLast3Months()
    {
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.SearchAllInvoicesAsync(It.IsAny<ICallerContext>(),
                It.IsAny<BillingProvider>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResults<Invoice>());

        var result = await _application.SearchSubscriptionHistoryAsync(_caller.Object, "anowningentityid", null, null,
            new SearchOptions(), new GetOptions(),
            CancellationToken.None);

        result.Should().BeSuccess();
        var now = DateTime.UtcNow;
        var threeMonthsAgo = now.Add(-Validations.Subscription.DefaultInvoicePeriod);
        _billingProvider.Verify(bp => bp.GatewayService.SearchAllInvoicesAsync(_caller.Object,
            It.IsAny<BillingProvider>(), It.Is<DateTime>(dt => dt.IsNear(threeMonthsAgo, TimeSpan.FromMinutes(1))),
            It.Is<DateTime>(dt => dt.IsNear(now, TimeSpan.FromMinutes(1))),
            It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchSubscriptionHistoryAsyncAndOnlyFromDate_ThenReturnsHistoryForNext3Months()
    {
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.SearchAllInvoicesAsync(It.IsAny<ICallerContext>(),
                It.IsAny<BillingProvider>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResults<Invoice>());
        var start = DateTime.UtcNow.SubtractHours(1);

        var result = await _application.SearchSubscriptionHistoryAsync(_caller.Object, "anowningentityid", start, null,
            new SearchOptions(), new GetOptions(),
            CancellationToken.None);

        result.Should().BeSuccess();
        var threeMonthsLater = start.Add(Validations.Subscription.DefaultInvoicePeriod);
        _billingProvider.Verify(bp => bp.GatewayService.SearchAllInvoicesAsync(_caller.Object,
            It.IsAny<BillingProvider>(), It.Is<DateTime>(dt => dt.IsNear(start, TimeSpan.FromMinutes(1))),
            It.Is<DateTime>(dt => dt.IsNear(threeMonthsLater, TimeSpan.FromMinutes(1))),
            It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchSubscriptionHistoryAsyncAndOnlyToDate_ThenReturnsHistoryForLast3Months()
    {
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.SearchAllInvoicesAsync(It.IsAny<ICallerContext>(),
                It.IsAny<BillingProvider>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResults<Invoice>());
        var end = DateTime.UtcNow.AddHours(1);

        var result = await _application.SearchSubscriptionHistoryAsync(_caller.Object, "anowningentityid", null, end,
            new SearchOptions(), new GetOptions(),
            CancellationToken.None);

        result.Should().BeSuccess();
        var threeMonthsSooner = end.Subtract(Validations.Subscription.DefaultInvoicePeriod);
        _billingProvider.Verify(bp => bp.GatewayService.SearchAllInvoicesAsync(_caller.Object,
            It.IsAny<BillingProvider>(), It.Is<DateTime>(dt => dt.IsNear(threeMonthsSooner, TimeSpan.FromMinutes(1))),
            It.Is<DateTime>(dt => dt.IsNear(end, TimeSpan.FromMinutes(1))),
            It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExportSubscriptionsToMigrateAsync_ThenReturnsSubscriptions()
    {
        _repository.Setup(rep =>
                rep.SearchAllByProviderAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<Subscription>([
                new Subscription
                {
                    Id = "asubscriptionid",
                    BuyerId = "abuyerid",
                    OwningEntityId = "anowningentityid",
                    ProviderName = "aprovidername",
                    ProviderState = new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    }.ToJson()
                }
            ]));
        _userProfilesService.Setup(usp =>
                usp.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                UserId = "abuyerid",
                Classification = UserProfileClassification.Person,
                EmailAddress = "anemailaddress",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                DisplayName = "adisplayname",
                Address = new ProfileAddress
                {
                    CountryCode = "acountrycode"
                },
                PhoneNumber = "aphonenumber"
            });

        var result = await _application.ExportSubscriptionsToMigrateAsync(_caller.Object, new SearchOptions(),
            new GetOptions(),
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("asubscriptionid");
        result.Value.Results[0].BuyerId.Should().Be("abuyerid");
        result.Value.Results[0].ProviderName.Should().Be("aprovidername");
        result.Value.Results[0].ProviderState.Should().Contain(pair => pair.Key == "aname" && pair.Value == "avalue");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.Id)].Should().Be("abuyerid");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.Name)].Should().Be("{\"FirstName\":\"afirstname\"}");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.EmailAddress)].Should().Be("anemailaddress");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.PhoneNumber)].Should().Be("aphonenumber");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.Address)].Should()
            .Be("{\"CountryCode\":\"acountrycode\"}");
        result.Value.Results[0].Buyer[nameof(SubscriptionBuyer.Subscriber)].Should()
            .Be("{\"EntityId\":\"anowningentityid\",\"EntityType\":\"Organization\"}");
        _userProfilesService.Verify(usp =>
            usp.GetProfilePrivateAsync(_caller.Object, "abuyerid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMigrateSubscriptionAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.MigrateSubscriptionAsync(_caller.Object, "anowningentityid", "aprovidername",
                new Dictionary<string, string>(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenMigrateSubscriptionAsyncByServiceAccount_ThenReturnsMigrates()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.MaintenanceAccountUserId);
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname1", "avalue1" } });
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
            .Returns("anewprovidername");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);
        var newProviderState = new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        };
        _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("anewbuyerreference");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("anewsubscriptionreference".ToOptional());

        var result = await _application.MigrateSubscriptionAsync(_caller.Object, "anowningentityid", "anewprovidername",
            newProviderState, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("abuyerid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("anewprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Provider.Value.Name == "anewprovidername"
            && root.Provider.Value.State.Count == 1
            && root.Provider.Value.State["aname2"] == "avalue2"
            && root.ProviderBuyerReference == "anewbuyerreference"
            && root.ProviderSubscriptionReference == "anewsubscriptionreference"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncWithUnknownOwningEntity_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.TransferSubscriptionAsync(_caller.Object, "anowningentityid", "auserid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _owningEntityService.Verify(
            oes => oes.CanTransferSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyer_ThenChangesPlan()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _caller.Setup(cc => cc.CallerId)
            .Returns("abuyerid");
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _billingProvider.Setup(bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<TransferSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Address = new ProfileAddress
                {
                    CountryCode = "acountrycode"
                },
                Classification = UserProfileClassification.Person,
                DisplayName = "adisplayname",
                EmailAddress = "auser@company.com",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });
        _owningEntityService.Setup(oes => oes.CanTransferSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Permission.Allowed);

        var result =
            await _application.TransferSubscriptionAsync(_caller.Object, "anowningentityid", "auserid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.BuyerId.Should().Be("auserid".ToId());
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername".ToId());
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "auserid".ToId()
            && root.ProviderBuyerReference == "abuyerreference"
            && root.ProviderSubscriptionReference == "asubscriptionreference"
        ), It.IsAny<CancellationToken>()));
        _owningEntityService.Verify(oes =>
            oes.CanTransferSubscriptionAsync(_caller.Object, "anowningentityid", "abuyerid", "auserid",
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc => cc.CallId == "acallid"), "auserid",
                It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.TransferSubscriptionAsync(It.IsAny<ICallerContext>(),
            It.Is<TransferSubscriptionOptions>(options =>
                options.TransfereeBuyer.Id == "auserid"
                && options.PlanId == null),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
    }
}