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
using Domain.Shared.EndUsers;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using Membership = Application.Resources.Shared.Membership;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;

namespace OrganizationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationsApplicationSubscriptionOwningEntitySpec
{
    private readonly OrganizationsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOrganizationRepository> _repository;
    private readonly Mock<ITenantSettingService> _tenantSettingService;

    public OrganizationsApplicationSubscriptionOwningEntitySpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anorganizationid".ToId());
        var tenantSettingsService = new Mock<ITenantSettingsService>();
        tenantSettingsService.Setup(tss =>
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
        var imagesService = new Mock<IImagesService>();
        _repository = new Mock<IOrganizationRepository>();
        _repository.Setup(ar => ar.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .Returns((OrganizationRoot root, CancellationToken _) =>
                Task.FromResult<Result<OrganizationRoot, Error>>(root));
        var subscriptionService = new Mock<ISubscriptionsService>();

        _application = new OrganizationsApplication(_recorder.Object, _identifierFactory.Object,
            tenantSettingsService.Object, _tenantSettingService.Object, _endUsersService.Object, imagesService.Object,
            subscriptionService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCanCancelSubscriptionAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CanCancelSubscriptionAsync(_caller.Object, "anorganizationid", "acancellerid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanCancelSubscriptionAsyncAndCancellerNotAMember_ThenReturnsError()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships = []
            });

        var result =
            await _application.CanCancelSubscriptionAsync(_caller.Object, "anorganizationid", "acancellerid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanCancelSubscriptionAsync_ThenReturnsPermission()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        organization.SubscribeBilling("asubscriptionid".ToId(), "acancellerid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships =
                [
                    new Membership
                    {
                        OrganizationId = "anorganizationid",
                        UserId = "auserid",
                        Id = "amembershipid"
                    }
                ]
            });

        var result =
            await _application.CanCancelSubscriptionAsync(_caller.Object, "anorganizationid", "acancellerid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsAllowed.Should().BeTrue();
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "acancellerid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenCanChangeSubscriptionPlanAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CanChangeSubscriptionPlanAsync(_caller.Object, "anorganizationid", "amodifierid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanChangeSubscriptionPlanAsyncAndModifierNotAMember_ThenReturnsError()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships = []
            });

        var result =
            await _application.CanChangeSubscriptionPlanAsync(_caller.Object, "anorganizationid", "amodifierid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanChangeSubscriptionPlanAsync_ThenReturnsPermission()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        organization.SubscribeBilling("asubscriptionid".ToId(), "amodifierid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships =
                [
                    new Membership
                    {
                        OrganizationId = "anorganizationid",
                        UserId = "auserid",
                        Id = "amembershipid"
                    }
                ]
            });

        var result =
            await _application.CanChangeSubscriptionPlanAsync(_caller.Object, "anorganizationid", "amodifierid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsAllowed.Should().BeTrue();
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "amodifierid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenCanTransferSubscriptionAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CanTransferSubscriptionAsync(_caller.Object, "anorganizationid", "atransfererid",
                "atransfereeid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanTransferSubscriptionAsyncAndTransfereeNotAMember_ThenReturnsError()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships = []
            });

        var result =
            await _application.CanTransferSubscriptionAsync(_caller.Object, "anorganizationid", "atransfererid",
                "atransfereeid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanTransferSubscriptionAsync_ThenReturnsPermission()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        organization.SubscribeBilling("asubscriptionid".ToId(), "atransfererid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships =
                [
                    new Membership
                    {
                        OrganizationId = "anorganizationid",
                        UserId = "auserid",
                        Id = "amembershipid",
                        Roles = [TenantRoles.BillingAdmin.Name]
                    }
                ]
            });

        var result =
            await _application.CanTransferSubscriptionAsync(_caller.Object, "anorganizationid", "atransfererid",
                "atransfereeid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsAllowed.Should().BeTrue();
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "atransfereeid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenCanUnsubscribeAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CanUnsubscribeAsync(_caller.Object, "anorganizationid", "anunsubsciberid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanUnsubscribeAsync_ThenReturnsPermission()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        organization.SubscribeBilling("asubscriptionid".ToId(), "anunsubsciberid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result =
            await _application.CanUnsubscribeAsync(_caller.Object, "anorganizationid", "anunsubsciberid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task WhenCanViewSubscriptionAsyncAndNotExists_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _application.CanViewSubscriptionAsync(_caller.Object, "anorganizationid", "aviewerid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanViewSubscriptionAsyncAndViewerNotAMember_ThenReturnsError()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value);
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships = []
            });

        var result =
            await _application.CanViewSubscriptionAsync(_caller.Object, "anorganizationid", "aviewerid",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCanViewSubscriptionAsync_ThenReturnsPermission()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "auserid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value).Value;
        organization.SubscribeBilling("asubscriptionid".ToId(), "aviewerid".ToId());
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Memberships =
                [
                    new Membership
                    {
                        OrganizationId = "anorganizationid",
                        UserId = "auserid",
                        Id = "amembershipid"
                    }
                ]
            });

        var result =
            await _application.CanViewSubscriptionAsync(_caller.Object, "anorganizationid", "aviewerid",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsAllowed.Should().BeTrue();
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "aviewerid", CancellationToken.None));
    }
}