using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;

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
    public async Task WhenCreateOrganizationAsync_ThenReturnsOrganization()
    {
        var result =
            await _application.CreateOrganizationAsync(_caller.Object, "auserid", "aname",
                OrganizationOwnership.Personal, CancellationToken.None);

        result.Value.Name.Should().Be("aname");
        result.Value.Ownership.Should().Be(OrganizationOwnership.Personal);
        result.Value.CreatedById.Should().Be("auserid");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(org =>
            org.Name == "aname"
            && org.Ownership == Ownership.Personal
            && org.CreatedById == "auserid"
            && org.Settings.Properties.Count == 1
            && org.Settings.Properties["aname"].Value.As<string>() == "avalue"
            && org.Settings.Properties["aname"].IsEncrypted == false
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
            tss.CreateForTenantAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateSharedOrganizationAsync_ThenReturnsSharedOrganization()
    {
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");
        _endUsersService.Setup(eus =>
                eus.CreateMembershipForCallerPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Membership
            {
                Id = "amembershipid",
                OrganizationId = "anorganizationid",
                IsDefault = false
            });

        var result =
            await _application.CreateSharedOrganizationAsync(_caller.Object, "aname",
                CancellationToken.None);

        result.Value.Name.Should().Be("aname");
        result.Value.Ownership.Should().Be(OrganizationOwnership.Shared);
        result.Value.CreatedById.Should().Be("acallerid");
        _repository.Verify(rep => rep.SaveAsync(It.Is<OrganizationRoot>(org =>
            org.Name == "aname"
            && org.Ownership == Ownership.Shared
            && org.CreatedById == "acallerid"
            && org.Settings.Properties.Count == 1
            && org.Settings.Properties["aname"].Value.As<string>() == "avalue"
            && org.Settings.Properties["aname"].IsEncrypted == false
        ), It.IsAny<CancellationToken>()));
        _tenantSettingsService.Verify(tss =>
            tss.CreateForTenantAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.CreateMembershipForCallerPrivateAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
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
            Ownership.Personal, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
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
        result.Value.Ownership.Should().Be(OrganizationOwnership.Personal);
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
            Ownership.Personal, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
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
            Ownership.Personal, "auserid".ToId(), DisplayName.Create("aname").Value).Value;
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
            && o.Ownership == Ownership.Personal
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
}