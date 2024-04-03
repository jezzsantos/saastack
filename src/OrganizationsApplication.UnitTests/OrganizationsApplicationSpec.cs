using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace OrganizationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationsApplicationSpec
{
    private readonly OrganizationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly Mock<ITenantSettingService> _tenantSettingService;
    private readonly Mock<ITenantSettingsService> _tenantSettingsService;

    public OrganizationsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
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
        _endUsersService = new Mock<IEndUsersService>();
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(ar => ar.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .Returns((OrganizationRoot root, CancellationToken _) =>
                Task.FromResult<Result<OrganizationRoot, Error>>(root));

        _application = new OrganizationsApplication(_recorder.Object, _idFactory.Object,
            _tenantSettingsService.Object, _tenantSettingService.Object, _endUsersService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCreateSharedOrganizationAsync_ThenReturnsSharedOrganization()
    {
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");

        var result =
            await _application.CreateSharedOrganizationAsync(_caller.Object, "aname",
                CancellationToken.None);

        result.Value.Name.Should().Be("aname");
        result.Value.Ownership.Should().Be(Application.Resources.Shared.OrganizationOwnership.Shared);
        result.Value.CreatedById.Should().Be("acallerid");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(org =>
            org.Name == "aname"
            && org.Ownership == OrganizationOwnership.Shared
            && org.CreatedById == "acallerid"
            && org.Settings.Properties.Count == 1
            && org.Settings.Properties["aname"].Value.As<string>() == "avalue"
            && org.Settings.Properties["aname"].IsEncrypted == false
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
            tss.CreateForTenantAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.GetOrganizationAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGet_ThenReturnsOrganization()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname", Setting.Create("avalue", true).Value }
        }).Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.GetOrganizationAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Value.Name.Should().Be("aname");
        result.Value.CreatedById.Should().Be("auserid");
        result.Value.Ownership.Should().Be(Application.Resources.Shared.OrganizationOwnership.Personal);
    }

    [Fact]
    public async Task WhenGetSettingsAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.GetSettingsAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetSettings_ThenReturnsSettings()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname", Setting.Create("avalue", true).Value }
        }).Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result = await _application.GetSettingsAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value["aname"].Value.Should().Be("avalue");
        result.Value["aname"].IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public async Task WhenChangeSettingsAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.ChangeSettingsAsync(_caller.Object, "anorganizationid",
            new TenantSettings(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeSettings_ThenReturnsSettings()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anoldvalue", true).Value },
            { "aname4", Setting.Create("anoldvalue", true).Value }
        }).Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result = await _application.ChangeSettingsAsync(_caller.Object, "anorganizationid",
            new TenantSettings(new Dictionary<string, object>
            {
                { "aname1", "anewvalue" },
                { "aname2", 99 },
                { "aname3", true }
            }), CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(o =>
            o.Name == "aname"
            && o.Ownership == OrganizationOwnership.Shared
            && o.CreatedById == "auserid"
            && o.Settings.Properties.Count == 4
            && o.Settings.Properties["aname1"].Value.As<string>() == "anewvalue"
            && o.Settings.Properties["aname2"].Value.As<double>().Equals(99D)
            && o.Settings.Properties["aname3"].Value.As<bool>() == true
            && o.Settings.Properties["aname4"].Value.As<string>() == "anoldvalue"
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
                tss.CreateForTenantAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenInviteMemberToOrganizationAsyncAndNotExist_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.InviteMemberToOrganizationAsync(_caller.Object, "anorganizationid",
            "auserid", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenInviteMemberToOrganizationAsync_ThenInvites()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles(Array.Empty<RoleLevel>(), new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _endUsersService.Setup(eus =>
                eus.InviteMemberToOrganizationPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Membership
            {
                Id = "amembershipid",
                UserId = "auserid",
                OrganizationId = "anorganizationid",
                IsDefault = false
            });

        var result = await _application.InviteMemberToOrganizationAsync(_caller.Object, "anorganizationid",
            "auserid", null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.CreatedById.Should().Be("auserid");
        result.Value.Ownership.Should().Be(Application.Resources.Shared.OrganizationOwnership.Shared);
        _endUsersService.Verify(eus =>
            eus.InviteMemberToOrganizationPrivateAsync(_caller.Object, "anorganizationid", "auserid", null,
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenListMembersForOrganizationAsyncAndNotExist_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.ListMembersForOrganizationAsync(_caller.Object, "anorganizationid",
            new SearchOptions(), new GetOptions(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenListMembersForOrganizationAsyncWithUnregisteredUser_ThenReturnsMemberships()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _endUsersService.Setup(eus => eus.ListMembershipsForOrganizationAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<SearchOptions>(), It.IsAny<GetOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResults<MembershipWithUserProfile>
            {
                Results =
                [
                    new MembershipWithUserProfile
                    {
                        Id = "amembershipid",
                        UserId = "auserid",
                        Status = EndUserStatus.Unregistered,
                        Roles = ["arole1", "arole2", "arole3"],
                        Features = ["afeature1", "afeature2", "afeature3"],
                        Profile = new UserProfile
                        {
                            Id = "aprofileid",
                            UserId = "auserid",
                            EmailAddress = "anemailaddress",
                            Name = new PersonName
                            {
                                FirstName = "anemailaddress"
                            },
                            DisplayName = "anemailaddress"
                        },
                        OrganizationId = "anorganizationid",
                        IsDefault = false
                    }
                ]
            });

        var result = await _application.ListMembersForOrganizationAsync(_caller.Object, "anorganizationid",
            new SearchOptions(), new GetOptions(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("amembershipid");
        result.Value.Results[0].UserId.Should().Be("auserid");
        result.Value.Results[0].IsRegistered.Should().BeFalse();
        result.Value.Results[0].IsOwner.Should().BeFalse();
        result.Value.Results[0].EmailAddress.Should().Be("anemailaddress");
        result.Value.Results[0].Name.FirstName.Should().Be("anemailaddress");
        result.Value.Results[0].Name.LastName.Should().BeNull();
        result.Value.Results[0].Roles.Should().ContainInOrder("arole1", "arole2", "arole3");
        result.Value.Results[0].Features.Should().ContainInOrder("afeature1", "afeature2", "afeature3");
    }

    [Fact]
    public async Task WhenListMembersForOrganizationAsyncWithRegisteredUsers_ThenReturnsMemberships()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _endUsersService.Setup(eus => eus.ListMembershipsForOrganizationAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<SearchOptions>(), It.IsAny<GetOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResults<MembershipWithUserProfile>
            {
                Results =
                [
                    new MembershipWithUserProfile
                    {
                        Id = "amembershipid",
                        UserId = "auserid",
                        Status = EndUserStatus.Registered,
                        Roles = ["arole1", "arole2", "arole3"],
                        Features = ["afeature1", "afeature2", "afeature3"],
                        OrganizationId = "anorganizationid",
                        Profile = new UserProfile
                        {
                            Id = "aprofileid",
                            UserId = "auserid",
                            EmailAddress = "anemailaddress",
                            Name = new PersonName
                            {
                                FirstName = "afirstname",
                                LastName = "alastname"
                            },
                            DisplayName = "adisplayname"
                        },
                        IsDefault = false
                    }
                ]
            });

        var result = await _application.ListMembersForOrganizationAsync(_caller.Object, "anorganizationid",
            new SearchOptions(), new GetOptions(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("amembershipid");
        result.Value.Results[0].UserId.Should().Be("auserid");
        result.Value.Results[0].IsRegistered.Should().BeTrue();
        result.Value.Results[0].IsOwner.Should().BeFalse();
        result.Value.Results[0].EmailAddress.Should().Be("anemailaddress");
        result.Value.Results[0].Name.FirstName.Should().Be("afirstname");
        result.Value.Results[0].Name.LastName.Should().Be("alastname");
        result.Value.Results[0].Roles.Should().ContainInOrder("arole1", "arole2", "arole3");
        result.Value.Results[0].Features.Should().ContainInOrder("afeature1", "afeature2", "afeature3");
    }
}