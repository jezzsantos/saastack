using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using Moq;
using OrganizationsDomain;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using UnitTesting.Common;
using Xunit;
using Events = OrganizationsDomain.Events;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionsApplicationDomainEventHandlersSpec
{
    private readonly SubscriptionsApplication _application;
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionRepository> _repository;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public SubscriptionsApplicationDomainEventHandlersSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _billingProvider = new Mock<IBillingProvider>();
        _billingProvider.Setup(bp => bp.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);
        _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
        _repository = new Mock<ISubscriptionRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, CancellationToken _) => root);

        _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
            _userProfilesService.Object, _billingProvider.Object, owningEntityService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenHandleOrganizationCreatedAsync_ThenReturnsOk()
    {
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Personal, "auserid".ToId(),
            DisplayName.Create("aname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "auserid".ToId()
            && root.OwningEntityId == "anorganizationid".ToId()
            && root.Provider.Value.Name == "aprovidername"
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "auserid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationDeletedAsync_ThenReturnsOk()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        var domainEvent = Events.Deleted("anowningentityid".ToId(), "adeleterid".ToId());
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());

        var result =
            await _application.HandleOrganizationDeletedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.IsDeleted == true
        ), It.IsAny<CancellationToken>()));
    }
}