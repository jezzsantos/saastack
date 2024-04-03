using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using Moq;
using OrganizationsDomain;
using UnitTesting.Common;
using Xunit;
using Events = OrganizationsDomain.Events;
using Membership = EndUsersDomain.Membership;

namespace EndUsersApplication.UnitTests;

[Trait("Category", "Unit")]
public class EndUsersApplicationDomainEventHandlersSpec
{
    private readonly EndUsersApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUserRepository> _endUserRepository;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public EndUsersApplicationDomainEventHandlersSpec()
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
        _endUserRepository = new Mock<IEndUserRepository>();
        _endUserRepository.Setup(rep => rep.SaveAsync(It.IsAny<EndUserRoot>(), It.IsAny<CancellationToken>()))
            .Returns((EndUserRoot root, CancellationToken _) => Task.FromResult<Result<EndUserRoot, Error>>(root));
        var invitationRepository = new Mock<IInvitationRepository>();
        var userProfilesService = new Mock<IUserProfilesService>();
        var notificationsService = new Mock<INotificationsService>();

        _application =
            new EndUsersApplication(_recorder.Object, _idFactory.Object, settings.Object, notificationsService.Object,
                userProfilesService.Object, invitationRepository.Object, _endUserRepository.Object);
    }

    [Fact]
    public async Task WhenHandleOrganizationCreatedAsyncAndUserNoExist_ThenReturnsError()
    {
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());
        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task HandleOrganizationCreatedAsync_ThenAddsMembership()
    {
        var user = EndUserRoot.Create(_recorder.Object, _idFactory.Object, UserClassification.Person).Value;
        user.Register(Roles.Create(PlatformRoles.Standard).Value, Features.Create(PlatformFeatures.Basic).Value,
            EndUserProfile.Create("afirstname").Value, EmailAddress.Create("auser@company.com").Value);
        _endUserRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var domainEvent = Events.Created("anorganizationid".ToId(), OrganizationOwnership.Shared,
            "auserid".ToId(), DisplayName.Create("adisplayname").Value);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _endUserRepository.Verify(rep => rep.SaveAsync(It.Is<EndUserRoot>(eu =>
            eu.Memberships[0].IsDefault
            && eu.Memberships[0].OrganizationId == "anorganizationid".ToId()
            && eu.Memberships[0].Roles.HasRole(TenantRoles.Member)
            && eu.Memberships[0].Features.HasFeature(TenantFeatures.Basic)
        ), It.IsAny<CancellationToken>()));
    }
}