using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Membership = EndUsersDomain.Membership;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<INotificationsService> _notificationsService;
    private readonly Mock<IOrganizationsService> _organizationsService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IEndUserRepository> _repository;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public EndUsersApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        var membershipCounter = 0;
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity entity) =>
            {
                if (entity is Membership)
                {
                    return $"amembershipid{membershipCounter++}".ToId();
                }

                return "anid".ToId();
            });
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(
                s => s.Platform.GetString(EndUsersApplication.PermittedOperatorsSettingName, It.IsAny<string?>()))
            .Returns("");
        _repository = new Mock<IEndUserRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        _organizationsService = new Mock<IOrganizationsService>();
        _organizationsService.Setup(os => os.CreateOrganizationPrivateAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OrganizationOwnership>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization
            {
                Id = "anorganizationid",
                CreatedById = "auserid",
                Name = "aname"
            });
        _userProfilesService = new Mock<IUserProfilesService>();
        _notificationsService = new Mock<INotificationsService>();

        _application =
            new EndUsersApplication(_recorder.Object, _idFactory.Object, settings.Object, _notificationsService.Object,
                _organizationsService.Object,
                _userProfilesService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenGetPersonAndUnregistered_ThenReturnsUser()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _application.GetPersonAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Unregistered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().BeEmpty();
        result.Value.Features.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenRegisterPersonAndNotAcceptedTerms_ThenReturnsError()
    {
        var result = await _application.RegisterPersonAsync(_caller.Object, "anemailaddress", "afirstname", "alastname",
            "atimezone", "acountrycode", false, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUsersApplication_NotAcceptedTerms);
    }

    [Fact]
    public async Task WhenRegisterPersonAndNotExist_ThenRegisters()
    {
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfile>.None);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<EndUserRoot>.None);
        _userProfilesService.Setup(ups =>
                ups.CreatePersonProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                Type = UserProfileType.Person,
                UserId = "apersonid",
                DisplayName = "afirstname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                EmailAddress = "auser@company.com",
                Timezone = "atimezone",
                Address =
                {
                    CountryCode = "acountrycode"
                }
            });

        var result = await _application.RegisterPersonAsync(_caller.Object, "auser@company.com", "afirstname",
            "alastname",
            Timezones.Default.ToString(), CountryCodes.Default.ToString(), true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
        result.Value.Profile!.Id.Should().Be("aprofileid");
        result.Value.Profile.DefaultOrganizationId.Should().Be("anorganizationid");
        result.Value.Profile.Address.CountryCode.Should().Be("acountrycode");
        result.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Value.Profile.EmailAddress.Should().Be("auser@company.com");
        result.Value.Profile.Timezone.Should().Be("atimezone");
        _organizationsService.Verify(os =>
            os.CreateOrganizationPrivateAsync(_caller.Object, "anid", "afirstname alastname",
                OrganizationOwnership.Personal,
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.CreatePersonProfilePrivateAsync(_caller.Object, "anid", "auser@company.com", "afirstname", "alastname",
                Timezones.Default.ToString(), CountryCodes.Default.ToString(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRegisterPersonAndInvitedAsGuest_ThenCompletesRegistration()
    {
        var invitee = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfile>.None);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.CreatePersonProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                Type = UserProfileType.Person,
                UserId = "apersonid",
                DisplayName = "afirstname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                EmailAddress = "auser@company.com",
                Timezone = "atimezone",
                Address =
                {
                    CountryCode = "acountrycode"
                }
            });

        var result = await _application.RegisterPersonAsync(_caller.Object, "auser@company.com", "afirstname",
            "alastname", Timezones.Default.ToString(), CountryCodes.Default.ToString(), true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
        result.Value.Profile!.Id.Should().Be("aprofileid");
        result.Value.Profile.DefaultOrganizationId.Should().Be("anorganizationid");
        result.Value.Profile.Address.CountryCode.Should().Be("acountrycode");
        result.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Value.Profile.EmailAddress.Should().Be("auser@company.com");
        result.Value.Profile.Timezone.Should().Be("atimezone");
        _organizationsService.Verify(os =>
            os.CreateOrganizationPrivateAsync(_caller.Object, "anid", "afirstname alastname",
                OrganizationOwnership.Personal,
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.CreatePersonProfilePrivateAsync(_caller.Object, "anid", "auser@company.com", "afirstname", "alastname",
                Timezones.Default.ToString(), CountryCodes.Default.ToString(), It.IsAny<CancellationToken>()));
        _notificationsService.Verify(ns => ns.NotifyReRegistrationCourtesyAsync(It.IsAny<ICallerContext>(),
            It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRegisterPersonAndAlreadyRegistered_ThenSendsCourtesyEmail()
    {
        var endUser = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        endUser.Register(Roles.Empty, Features.Empty, EmailAddress.Create("auser@company.com").Value);
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                Type = UserProfileType.Person,
                UserId = "auserid",
                DisplayName = "afirstname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                EmailAddress = "anotheruser@company.com",
                Address =
                {
                    CountryCode = "acountrycode"
                },
                Timezone = "atimezone"
            }.ToOptional());
        endUser.AddMembership("anorganizationid".ToId(), Roles.Empty, Features.Empty);
        _repository.Setup(rep =>
                rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(endUser);
        _notificationsService.Setup(ns =>
                ns.NotifyReRegistrationCourtesyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.RegisterPersonAsync(_caller.Object, "auser@company.com", "afirstname",
            "alastname", Timezones.Default.ToString(), CountryCodes.Default.ToString(), true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().BeEmpty();
        result.Value.Features.Should().BeEmpty();
        result.Value.Profile!.Id.Should().Be("aprofileid");
        result.Value.Profile.DefaultOrganizationId.Should().Be("amembershipid0");
        result.Value.Profile.Address.CountryCode.Should().Be("acountrycode");
        result.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Value.Profile.EmailAddress.Should().Be("anotheruser@company.com");
        result.Value.Profile.Timezone.Should().Be("atimezone");
        _organizationsService.Verify(os =>
            os.CreateOrganizationPrivateAsync(_caller.Object, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OrganizationOwnership>(), It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.CreatePersonProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _notificationsService.Verify(ns => ns.NotifyReRegistrationCourtesyAsync(_caller.Object, "anid",
            "anotheruser@company.com", "afirstname", "atimezone", "acountrycode", CancellationToken.None));
    }

    [Fact]
    public async Task WhenRegisterMachineByAnonymousUser_ThenRegistersWithNoFeatures()
    {
        _userProfilesService.Setup(ups =>
                ups.CreateMachineProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                Type = UserProfileType.Machine,
                UserId = "amachineid",
                DisplayName = "amachinename",
                Name = new PersonName
                {
                    FirstName = "amachinename"
                },
                Timezone = "atimezone",
                Address =
                {
                    CountryCode = "acountrycode"
                }
            });
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _application.RegisterMachineAsync(_caller.Object, "aname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Machine);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.Basic.Name);
        result.Value.Profile!.Id.Should().Be("aprofileid");
        result.Value.Profile.DefaultOrganizationId.Should().Be("anorganizationid");
        result.Value.Profile.Address.CountryCode.Should().Be("acountrycode");
        result.Value.Profile.Name.FirstName.Should().Be("amachinename");
        result.Value.Profile.Name.LastName.Should().BeNull();
        result.Value.Profile.DisplayName.Should().Be("amachinename");
        result.Value.Profile.EmailAddress.Should().BeNull();
        result.Value.Profile.Timezone.Should().Be("atimezone");
        _organizationsService.Verify(os =>
            os.CreateOrganizationPrivateAsync(_caller.Object, "anid", "aname", OrganizationOwnership.Personal,
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.CreateMachineProfilePrivateAsync(_caller.Object, "anid", "aname", Timezones.Default.ToString(),
                CountryCodes.Default.ToString(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRegisterMachineByAuthenticatedUser_ThenRegistersWithBasicFeatures()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var adder = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adder);
        adder.Register(Roles.Empty, Features.Empty, EmailAddress.Create("auser@company.com").Value);
        adder.AddMembership("anotherorganizationid".ToId(), Roles.Empty, Features.Empty);
        _userProfilesService.Setup(ups =>
                ups.CreateMachineProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                Type = UserProfileType.Machine,
                UserId = "amachineid",
                DisplayName = "amachinename",
                Name = new PersonName
                {
                    FirstName = "amachinename"
                },
                Timezone = "atimezone",
                Address =
                {
                    CountryCode = "acountrycode"
                }
            });

        var result = await _application.RegisterMachineAsync(_caller.Object, "aname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Machine);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
        result.Value.Profile!.Id.Should().Be("aprofileid");
        result.Value.Profile.DefaultOrganizationId.Should().Be("anorganizationid");
        result.Value.Profile.Address.CountryCode.Should().Be("acountrycode");
        result.Value.Profile.Name.FirstName.Should().Be("amachinename");
        result.Value.Profile.Name.LastName.Should().BeNull();
        result.Value.Profile.DisplayName.Should().Be("amachinename");
        result.Value.Profile.EmailAddress.Should().BeNull();
        result.Value.Profile.Timezone.Should().Be("atimezone");
        _organizationsService.Verify(os =>
            os.CreateOrganizationPrivateAsync(_caller.Object, "anid", "aname", OrganizationOwnership.Personal,
                It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.CreateMachineProfilePrivateAsync(_caller.Object, "anid", "aname", Timezones.Default.ToString(),
                CountryCodes.Default.ToString(),
                It.IsAny<CancellationToken>()));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenAssignPlatformRolesAsync_ThenAssigns()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("anassignerid");
        var assignee = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assignee.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            Optional<EmailAddress>.None);
        _repository.Setup(rep => rep.LoadAsync("anassigneeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(), Optional<EmailAddress>.None);
        _repository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigner);

        var result = await _application.AssignPlatformRolesAsync(_caller.Object, "anassigneeid",
            [PlatformRoles.TestingOnly.Name],
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Roles.Should().ContainInOrder(PlatformRoles.Standard.Name, PlatformRoles.TestingOnly.Name);
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenAssignTenantRolesAsync_ThenAssigns()
    {
        _caller.Setup(cc => cc.CallerId)
            .Returns("anassignerid");
        var assignee = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assignee.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            Optional<EmailAddress>.None);
        assignee.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _repository.Setup(rep => rep.LoadAsync("anassigneeid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(), Optional<EmailAddress>.None);
        assigner.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _repository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigner);

        var result = await _application.AssignTenantRolesAsync(_caller.Object, "anorganizationid", "anassigneeid",
            [TenantRoles.TestingOnly.Name],
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Roles.Should().ContainInOrder(PlatformRoles.Standard.Name);
        result.Value.Memberships[0].Roles.Should()
            .ContainInOrder(TenantRoles.Member.Name, TenantRoles.TestingOnly.Name);
    }
#endif

    [Fact]
    public async Task WhenFindPersonByEmailAsyncAndNotExists_ThenReturnsNone()
    {
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfile>.None);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional.None<EndUserRoot>());

        var result =
            await _application.FindPersonByEmailAddressAsync(_caller.Object, "auser@company.com",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenFindPersonByEmailAsyncAndExists_ThenReturns()
    {
        var endUser = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _userProfilesService.Setup(ups =>
                ups.FindPersonByEmailAddressPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<UserProfile>.None);
        _repository.Setup(rep =>
                rep.FindInvitedGuestByEmailAddressAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(endUser.ToOptional());

        var result =
            await _application.FindPersonByEmailAddressAsync(_caller.Object, "auser@company.com",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Value.Id.Should().Be("anid");
    }

    [Fact]
    public async Task WhenGetMembershipsAndNotRegisteredOrMemberAsync_ThenReturnsUser()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _application.GetMembershipsAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Unregistered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().BeEmpty();
        result.Value.Features.Should().BeEmpty();
        result.Value.Memberships.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenGetMembershipsAsync_ThenReturnsUser()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        user.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EmailAddress.Create("auser@company.com").Value);
        user.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Member).Value,
            Features.Create(TenantFeatures.PaidTrial).Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _application.GetMembershipsAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Person);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.Basic.Name);
        result.Value.Memberships.Count.Should().Be(1);
        result.Value.Memberships[0].IsDefault.Should().BeTrue();
        result.Value.Memberships[0].OrganizationId.Should().Be("anorganizationid");
        result.Value.Memberships[0].Roles.Should().ContainSingle(role => role == TenantRoles.Member.Name);
        result.Value.Memberships[0].Features.Should().ContainSingle(feat => feat == TenantFeatures.PaidTrial.Name);
    }

    [Fact]
    public async Task WhenCreateMembershipForCallerAsyncAndUserNoExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CreateMembershipForCallerAsync(_caller.Object, "anorganizationid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCreateMembershipForCallerAsync_ThenAddsMembership()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        user.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EmailAddress.Create("auser@company.com").Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result =
            await _application.CreateMembershipForCallerAsync(_caller.Object, "anorganizationid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsDefault.Should().BeTrue();
        result.Value.OrganizationId.Should().Be("anorganizationid");
        result.Value.Roles.Should().ContainSingle(role => role == TenantRoles.Member.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == TenantFeatures.Basic.Name);
    }
}