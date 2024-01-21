using Application.Interfaces;
using Application.Resources.Shared;
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

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IEndUserRepository> _repository;

    public EndUsersApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(
                s => s.Platform.GetString(EndUsersApplication.PermittedOperatorsSettingName, It.IsAny<string?>()))
            .Returns("");
        _repository = new Mock<IEndUserRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        _application =
            new EndUsersApplication(_recorder.Object, _idFactory.Object, settings.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenGetPersonAndUnregistered_ThenReturnsUser()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(user));

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
    public async Task WhenRegisterPerson_ThenRegisters()
    {
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
        result.Value.Profile!.Id.Should().Be("anid");
        result.Value.Profile.DefaultOrganisationId.Should().BeNull();
        result.Value.Profile.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Profile.Name.FirstName.Should().Be("afirstname");
        result.Value.Profile.Name.LastName.Should().Be("alastname");
        result.Value.Profile.DisplayName.Should().Be("afirstname");
        result.Value.Profile.EmailAddress.Should().Be("auser@company.com");
        result.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
    }

    [Fact]
    public async Task WhenRegisterMachineByAnonymousUser_ThenRegistersWithNoFeatures()
    {
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
        result.Value.Profile!.Id.Should().Be("anid");
        result.Value.Profile.DefaultOrganisationId.Should().BeNull();
        result.Value.Profile.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Profile.Name.FirstName.Should().Be("aname");
        result.Value.Profile.Name.LastName.Should().BeNull();
        result.Value.Profile.DisplayName.Should().Be("aname");
        result.Value.Profile.EmailAddress.Should().BeNull();
        result.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
    }

    [Fact]
    public async Task WhenRegisterMachineByAuthenticatedUser_ThenRegistersWithBasicFeatures()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);

        var result = await _application.RegisterMachineAsync(_caller.Object, "aname", Timezones.Default.ToString(),
            CountryCodes.Default.ToString(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Access.Should().Be(EndUserAccess.Enabled);
        result.Value.Status.Should().Be(EndUserStatus.Registered);
        result.Value.Classification.Should().Be(EndUserClassification.Machine);
        result.Value.Roles.Should().ContainSingle(role => role == PlatformRoles.Standard.Name);
        result.Value.Features.Should().ContainSingle(feat => feat == PlatformFeatures.PaidTrial.Name);
        result.Value.Profile!.Id.Should().Be("anid");
        result.Value.Profile.DefaultOrganisationId.Should().BeNull();
        result.Value.Profile.Address!.CountryCode.Should().Be(CountryCodes.Default.ToString());
        result.Value.Profile.Name.FirstName.Should().Be("aname");
        result.Value.Profile.Name.LastName.Should().BeNull();
        result.Value.Profile.DisplayName.Should().Be("aname");
        result.Value.Profile.EmailAddress.Should().BeNull();
        result.Value.Profile.Timezone.Should().Be(Timezones.Default.ToString());
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
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(assignee));
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(), Optional<EmailAddress>.None);
        _repository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(assigner));

        var result = await _application.AssignPlatformRolesAsync(_caller.Object, "anassigneeid",
            new List<string> { PlatformRoles.TestingOnly.Name },
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
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(assignee));
        var assigner = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        assigner.Register(Roles.Create(PlatformRoles.Operations).Value, Features.Create(), Optional<EmailAddress>.None);
        assigner.AddMembership("anorganizationid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Features.Create(TenantFeatures.Basic).Value);
        _repository.Setup(rep => rep.LoadAsync("anassignerid".ToId(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserRoot, Error>>(assigner));

        var result = await _application.AssignTenantRolesAsync(_caller.Object, "anorganizationid", "anassigneeid",
            new List<string> { TenantRoles.TestingOnly.Name },
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Roles.Should().ContainInOrder(PlatformRoles.Standard.Name);
        result.Value.Memberships[0].Roles.Should()
            .ContainInOrder(TenantRoles.Member.Name, TenantRoles.TestingOnly.Name);
    }
#endif
}