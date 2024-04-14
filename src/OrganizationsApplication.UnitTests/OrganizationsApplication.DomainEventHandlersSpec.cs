using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
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
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly Mock<ITenantSettingsService> _tenantSettingsService;

    public OrganizationsApplicationDomainEventHandlersSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tenantSettingsService = new Mock<ITenantSettingsService>();
        _tenantSettingsService.Setup(tss =>
                tss.CreateForTenantAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSettings(new Dictionary<string, object>
            {
                { "aname", "avalue" }
            }));
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        var endUsersService = new Mock<IEndUsersService>();
        var imagesService = new Mock<IImagesService>();
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(ar => ar.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .Returns((OrganizationRoot root, CancellationToken _) =>
                Task.FromResult<Result<OrganizationRoot, Error>>(root));

        _application = new OrganizationsApplication(recorder.Object, idFactory.Object,
            _tenantSettingsService.Object, tenantSettingService.Object, endUsersService.Object, imagesService.Object,
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
}