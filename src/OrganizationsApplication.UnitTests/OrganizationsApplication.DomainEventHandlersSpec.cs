using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Images;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersDomain;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using Events = EndUsersDomain.Events;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace OrganizationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationsApplicationDomainEventHandlersSpec
{
    private readonly OrganizationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IImagesService> _imagesService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly Mock<ITenantSettingService> _tenantSettingService;
    private readonly Mock<ITenantSettingsService> _tenantSettingsService;

    public OrganizationsApplicationDomainEventHandlersSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tenantSettingsService = new Mock<ITenantSettingsService>();
        _tenantSettingsService.Setup(tss =>
                tss.CreateForTenantAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSettings(new Dictionary<string, object>
            {
                { "aname", "avalue" }
            }));
        _tenantSettingService = new Mock<ITenantSettingService>();
        _tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        var endUsersService = new Mock<IEndUsersService>();
        _imagesService = new Mock<IImagesService>();
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(ar => ar.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .Returns((OrganizationRoot root, CancellationToken _) =>
                Task.FromResult<Result<OrganizationRoot, Error>>(root));

        _application = new OrganizationsApplication(_recorder.Object, _identifierFactory.Object,
            _tenantSettingsService.Object, _tenantSettingService.Object, endUsersService.Object, _imagesService.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredForPersonAsync_ThenReturnsOrganization()
    {
        var domainEvent = Events.Registered("auserid".ToId(), EndUserProfile.Create("afirstname", "alastname").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Person, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(org =>
            org.Name == "afirstname alastname"
            && org.Ownership == OrganizationOwnership.Personal
            && org.CreatedById == "auserid"
            && org.Settings.Properties.Count == 1
            && org.Settings.Properties["aname"].Value.As<string>() == "avalue"
            && org.Settings.Properties["aname"].IsEncrypted == false
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
            tss.CreateForTenantAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserRegisteredForMachineAsync_ThenReturnsOrganization()
    {
        var domainEvent = Events.Registered("auserid".ToId(), EndUserProfile.Create("amachinename").Value,
            EmailAddress.Create("auser@company.com").Value, UserClassification.Machine, UserAccess.Enabled,
            UserStatus.Registered, Roles.Empty, Features.Empty);

        var result =
            await _application.HandleEndUserRegisteredAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(org =>
            org.Name == "amachinename"
            && org.Ownership == OrganizationOwnership.Personal
            && org.CreatedById == "auserid"
            && org.Settings.Properties.Count == 1
            && org.Settings.Properties["aname"].Value.As<string>() == "avalue"
            && org.Settings.Properties["aname"].IsEncrypted == false
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
            tss.CreateForTenantAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserMembershipAddedAsync_ThenAddsMembership()
    {
        var domainEvent = Events.MembershipAdded("auserid".ToId(), "anorganizationid".ToId(),
            OrganizationOwnership.Shared, false, Roles.Empty, Features.Empty);
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "anownerid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.HandleEndUserMembershipAddedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(root =>
            root.Memberships.Members.Count == 1
            && root.Memberships.Members[0].UserId == "auserid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleEndUserMembershipRemovedAsync_ThenRemovesMembership()
    {
        var domainEvent = Events.MembershipRemoved("auserid".ToId(), "amembershipid".ToId(), "anorganizationid".ToId(),
            "auserid".ToId());
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "anownerid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        org.AddMembership("auserid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.HandleEndUserMembershipRemovedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(root =>
            root.Memberships.Members.Count == 0
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleImageDeletedAsync_ThenRemovesAvatarImage()
    {
        var domainEvent = new Deleted("animageid".ToId(), "auserid".ToId());
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "anownerid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        _repository.Setup(rep => rep.FindByAvatarIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org.ToOptional());

        var result = await _application.HandleImageDeletedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(up =>
            up.Id == "anid".ToId()
            && !up.Avatar.HasValue
        ), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.FindByAvatarIdAsync("animageid".ToId(), It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            img => img.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}