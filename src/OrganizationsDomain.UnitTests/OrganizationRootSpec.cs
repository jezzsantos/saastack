using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.Organizations;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationRootSpec
{
    private readonly OrganizationRoot _org;

    public OrganizationRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());
        var tenantSettingService = new Mock<ITenantSettingService>();
        tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);

        _org = OrganizationRoot.Create(recorder.Object, identifierFactory.Object, tenantSettingService.Object,
            OrganizationOwnership.Personal, "acreatorid".ToId(), DisplayName.Create("aname").Value).Value;
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        _org.Name.Name.Should().Be("aname");
        _org.CreatedById.Should().Be("acreatorid".ToId());
        _org.Ownership.Should().Be(OrganizationOwnership.Personal);
        _org.Settings.Should().Be(Settings.Empty);
    }

    [Fact]
    public void WhenCreateSettings_ThenAddsSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("avalue1", true).Value },
            { "aname2", Setting.Create("avalue2", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(2);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("avalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("avalue2", true).Value);
        _org.Events.Last().Should().BeOfType<SettingCreated>();
    }

    [Fact]
    public void WhenUpdateSettings_ThenAddsAndUpdatesSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anoldvalue1", false).Value },
            { "aname2", Setting.Create("anoldvalue2", false).Value }
        }).Value);
        _org.UpdateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anewvalue1", true).Value },
            { "aname3", Setting.Create("anewvalue3", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(3);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("anewvalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("anoldvalue2", false).Value);
        _org.Settings.Properties["aname3"].Should().Be(Setting.Create("anewvalue3", true).Value);
        _org.Events[3].Should().BeOfType<SettingUpdated>();
        _org.Events.Last().Should().BeOfType<SettingCreated>();
    }

    [Fact]
    public void WhenAddMembershipAndInviterNotOwner_ThenReturnsError()
    {
        var result = _org.AddMembership("aninviterid".ToId(), Roles.Empty, Optional<Identifier>.None,
            Optional<EmailAddress>.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_NotOrgOwner);
    }

    [Fact]
    public void WhenAddMembershipAndNoUser_ThenReturnsError()
    {
        var result = _org.AddMembership("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Optional<Identifier>.None, Optional<EmailAddress>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.OrganizationRoot_AddMembership_UserIdAndEmailMissing);
    }

    [Fact]
    public void WhenAddMembershipWithUserId_ThenAddsMembership()
    {
        var result = _org.AddMembership("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            "auserid".ToId(), Optional<EmailAddress>.None);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<MembershipAdded>();
    }

    [Fact]
    public void WhenAddMembershipWithEmailAddress_ThenAddsMembership()
    {
        var result = _org.AddMembership("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Optional<Identifier>.None, EmailAddress.Create("auser@company.com").Value);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<MembershipAdded>();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _org.ChangeAvatarAsync("anotheruserid".ToId(), Roles.Empty,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_NotOrgOwner);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNoExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        var result = await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().BeNull();
        result.Should().BeSuccess();
        _org.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _org.Avatar.Value.Url.Should().Be("aurl");
        _org.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndHasExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _org.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _org.Avatar.Value.Url.Should().Be("aurl");
        _org.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _org.DeleteAvatarAsync("anotheruserid".ToId(), Roles.Empty, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_NotOrgOwner);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNoExistingAvatar_ThenReturnsError()
    {
        var result = await _org.DeleteAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_NoAvatar);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndHasExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _org.DeleteAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value, id =>
        {
            imageDeletedId = id;
            return Task.FromResult(Result.Ok);
        });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _org.Avatar.HasValue.Should().BeFalse();
        _org.Events.Last().Should().BeOfType<AvatarRemoved>();
    }
}