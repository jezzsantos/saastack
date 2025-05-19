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
using UserProfilesDomain;
using Xunit;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using OrganizationsDomainEvents = OrganizationsDomain.Events;
using UserProfileEvents = Domain.Events.Shared.UserProfiles;

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
    public async Task WhenHandleOrganizationCreatedAsyncAndProfileNotExists_ThenCreatesPartialSubscription()
    {
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = OrganizationsDomainEvents.Created("anowningentityid".ToId(), OrganizationOwnership.Personal,
            "abuyerid".ToId(), DisplayName.Create("aname").Value, DatacenterLocations.Local);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.HasValue == false
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "abuyerid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenHandleOrganizationCreatedAsyncAndProfileExists_ThenCreatesCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        _repository.Setup(r =>
                r.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = "anemailaddress",
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "abuyerid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = OrganizationsDomainEvents.Created("anowningentityid".ToId(), OrganizationOwnership.Personal,
            "abuyerid".ToId(), DisplayName.Create("aname").Value, DatacenterLocations.Local);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "abuyerid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenHandleHandleUserProfileCreatedAsyncAndSubscriptionNotExists_ThenIgnores()
    {
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = "anemailaddress",
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ps => ps.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenHandleHandleUserProfileCreatedAsyncAndPartialSubscriptionExists_ThenCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "auserid".ToId(), stateInterpreter.Object).Value;
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = "anemailaddress",
                PhoneNumber = "aphonenumber",
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

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "auserid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "auserid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenHandleHandleUserProfileCreatedAsyncAndCompletedSubscriptionExists_ThenIgnores()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "auserid".ToId(), stateInterpreter.Object).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "auserid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ps => ps.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationDeletedAsync_ThenReturnsOk()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        var domainEvent = OrganizationsDomainEvents.Deleted("anowningentityid".ToId(), "adeleterid".ToId());
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