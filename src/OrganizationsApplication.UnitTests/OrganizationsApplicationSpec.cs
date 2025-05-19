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
using Domain.Shared;
using Domain.Shared.EndUsers;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using PersonName = Application.Resources.Shared.PersonName;

namespace OrganizationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationsApplicationSpec
{
    private readonly OrganizationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IImagesService> _imagesService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly Mock<ISubscriptionsService> _subscriptionsService;
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
        _imagesService = new Mock<IImagesService>();
        _subscriptionsService = new Mock<ISubscriptionsService>();
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(ar => ar.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .Returns((OrganizationRoot root, CancellationToken _) =>
                Task.FromResult<Result<OrganizationRoot, Error>>(root));

        _application = new OrganizationsApplication(_recorder.Object, _idFactory.Object, _tenantSettingsService.Object,
            _tenantSettingService.Object, _endUsersService.Object, _imagesService.Object, _subscriptionsService.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenCreateSharedOrganizationAsyncForPerson_ThenReturnsSharedOrganization()
    {
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "acallerid",
                Classification = EndUserClassification.Person
            });

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
    public async Task WhenGetAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.GetOrganizationAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetAsync_ThenReturnsOrganization()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
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
    public async Task WhenGetSettingsAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.GetSettingsAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetSettingsAsync_ThenReturnsSettings()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
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
    public async Task WhenChangeSettingsAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.ChangeSettingsAsync(_caller.Object, "anorganizationid",
            new TenantSettings(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeSettingsAsync_ThenReturnsSettings()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
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
    public async Task WhenInviteMemberToOrganizationAsyncAndNoUserIdNorEmail_ThenReturnsError()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result = await _application.InviteMemberToOrganizationAsync(_caller.Object, "anorganizationid",
            null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationApplication_InvitedNoUserNorEmail);
    }

    [Fact]
    public async Task WhenInviteMemberToOrganizationAsyncWithUserEmail_ThenInvites()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result = await _application.InviteMemberToOrganizationAsync(_caller.Object, "anorganizationid",
            null, "auser@company.com", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.CreatedById.Should().Be("auserid");
        result.Value.Ownership.Should().Be(Application.Resources.Shared.OrganizationOwnership.Shared);
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(o =>
            o.Id == "anid"
            && o.Memberships.Count == 0
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenInviteMemberToOrganizationAsyncWithUserId_ThenInvites()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result = await _application.InviteMemberToOrganizationAsync(_caller.Object, "anorganizationid",
            "auserid", null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.CreatedById.Should().Be("auserid");
        result.Value.Ownership.Should().Be(Application.Resources.Shared.OrganizationOwnership.Shared);
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(o =>
            o.Id == "anid"
            && o.Memberships.Count == 0
        ), It.IsAny<CancellationToken>()));
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
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
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
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
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

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" }
        };

        var result =
            await _application.ChangeAvatarAsync(_caller.Object, "anorganizationid", upload, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNoExistingAvatar_ThenReturnsOrganization()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" }
        };
        _imagesService.Setup(isv =>
                isv.CreateImageAsync(It.IsAny<ICallerContext>(), It.IsAny<FileUpload>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                ContentType = "acontenttype",
                Description = "adescription",
                Filename = "afilename",
                Url = "aurl",
                Id = "animageid"
            });

        var result =
            await _application.ChangeAvatarAsync(_caller.Object, "anorganizationid", upload, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(profile =>
            profile.Avatar.Value.ImageId == "animageid".ToId()
            && profile.Avatar.Value.Url == "aurl"
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(isv =>
            isv.CreateImageAsync(_caller.Object, upload, "aname", It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndExistingAvatar_ThenReturnsOrganization()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        await org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        var upload = new FileUpload
        {
            Content = new MemoryStream(),
            ContentType = new FileUploadContentType { MediaType = "acontenttype" }
        };
        _imagesService.Setup(isv =>
                isv.CreateImageAsync(It.IsAny<ICallerContext>(), It.IsAny<FileUpload>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                ContentType = "acontenttype",
                Description = "adescription",
                Filename = "afilename",
                Url = "aurl",
                Id = "animageid"
            });
        _imagesService.Setup(isv =>
                isv.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.ChangeAvatarAsync(_caller.Object, "anorganizationid", upload, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().Be("aurl");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(profile =>
            profile.Avatar.Value.ImageId == "animageid".ToId()
            && profile.Avatar.Value.Url == "aurl"
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(isv =>
            isv.CreateImageAsync(_caller.Object, upload, "aname", It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(_caller.Object, "anoldimageid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.DeleteAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles());
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.DeleteAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsync_ThenReturnsOrganization()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        await org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _imagesService.Setup(isv =>
                isv.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.DeleteAvatarAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AvatarUrl.Should().BeNull();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(profile =>
            profile.Avatar.HasValue == false
        ), It.IsAny<CancellationToken>()));
        _imagesService.Verify(
            isv => isv.DeleteImageAsync(_caller.Object, "anoldimageid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenChangeDetailsAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.ChangeDetailsAsync(_caller.Object, "auserid", "aname", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChangeDetailsAsyncAndNotOwner_ThenReturnsError()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles());
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.ChangeDetailsAsync(_caller.Object, "auserid", "aname", CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation);
    }

    [Fact]
    public async Task WhenChangeDetailsAsync_ThenReturnsOrganization()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        await org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _imagesService.Setup(isv =>
                isv.DeleteImageAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.ChangeDetailsAsync(_caller.Object, "auserid", "anewname", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("anewname");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(profile =>
            profile.Name == "anewname"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenUnInviteMemberFromOrganizationAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.UnInviteMemberFromOrganizationAsync(_caller.Object, "anid", "auserid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenUnInviteMemberFromOrganizationAsync_ThenRemovesMembership()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Optional<EmailAddress>.None);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.UnInviteMemberFromOrganizationAsync(_caller.Object, "anid", "auserid",
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(o =>
            o.Memberships.Count == 0
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAssignRolesToOrganizationAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.AssignRolesToOrganizationAsync(_caller.Object, "anorganizationid", "auserid",
                new List<string>(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenAssignRolesToOrganizationAsync_ThenAssigns()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        org.AddMembership("auserid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.AssignRolesToOrganizationAsync(_caller.Object, "anorganizationid", "auserid",
                new List<string>(), CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(root =>
            root.Id == "anid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenUnassignRolesFromOrganizationAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.UnassignRolesFromOrganizationAsync(_caller.Object, "anorganizationid", "auserid",
                new List<string>(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenUnassignRolesFromOrganizationAsync_ThenUnassigns()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        org.AddMembership("auserid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var result =
            await _application.UnassignRolesFromOrganizationAsync(_caller.Object, "anorganizationid", "auserid",
                new List<string>(), CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(root =>
            root.Id == "anid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteOrganizationAsyncAndNotExist_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.DeleteOrganizationAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteOrganizationAsync_ThenDeletes()
    {
        _caller.Setup(cc => cc.Roles)
            .Returns(new ICallerContext.CallerRoles([], new[] { TenantRoles.Owner }));
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        var org = OrganizationRoot.Create(_recorder.Object, _idFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        org.SubscribeBilling("asubscriptionid".ToId(), "acallerid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _subscriptionsService.Setup(ss =>
                ss.GetSubscriptionAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionWithPlan
            {
                Id = "asubscriptionid",
                Invoice = new InvoiceSummary
                {
                    Currency = "acurrency"
                },
                PaymentMethod = new SubscriptionPaymentMethod(),
                Period = new PlanPeriod(),
                Plan = new SubscriptionPlan
                {
                    Id = "aplanid"
                },
                SubscriptionReference = "asubscriptionreference",
                BuyerReference = "abuyerreference",
                BuyerId = "abuyerid",
                OwningEntityId = "anowningentityid",
                CanBeUnsubscribed = true
            });

        var result =
            await _application.DeleteOrganizationAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(root =>
            root.IsDeleted
        ), It.IsAny<CancellationToken>()));
    }
}