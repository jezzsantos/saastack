using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.UserProfiles;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace UserProfilesDomain.UnitTests;

[Trait("Category", "Unit")]
public class UserProfileRootSpec
{
    private readonly UserProfileRoot _profile;

    public UserProfileRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _profile = UserProfileRoot.Create(recorder.Object, identifierFactory.Object, ProfileType.Person,
            "auserid".ToId(), PersonName.Create("afirstname", "alastname").Value).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _profile.UserId.Should().Be("auserid".ToId());
        _profile.Type.Should().Be(ProfileType.Person);
        _profile.Name.Value.FirstName.Text.Should().Be("afirstname");
        _profile.Name.Value.LastName.Value.Text.Should().Be("alastname");
        _profile.DisplayName.Value.Text.Should().Be("afirstname");
        _profile.EmailAddress.HasValue.Should().BeFalse();
        _profile.Address.Should().Be(Address.Default);
        _profile.Timezone.Should().Be(Timezone.Default);
        _profile.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenSetEmailAddressAndNotOwner_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = _profile.SetEmailAddress("anotheruserid".ToId(), emailAddress);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenSetEmailAddressAndNotAPerson_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
#if TESTINGONLY
        _profile.TestingOnly_ChangeType(ProfileType.Machine);
#endif

        var result = _profile.SetEmailAddress("auserid".ToId(), emailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfileRoot_NotAPerson);
    }

    [Fact]
    public void WhenSetEmailAddress_ThenSets()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = _profile.SetEmailAddress("auserid".ToId(), emailAddress);

        result.Should().BeSuccess();
        _profile.EmailAddress.Value.Should().Be(emailAddress);
        _profile.Events.Last().Should().BeOfType<EmailAddressChanged>();
    }

    [Fact]
    public void WhenSetContactAddressAndNotOwner_ThenReturnsError()
    {
        var address = Address.Create(CountryCodes.Default).Value;

        var result = _profile.SetContactAddress("anotheruserid".ToId(), address);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenSetContactAddress_ThenSets()
    {
        var address = Address.Create("aline1", "aline2", "aline3", "acity", "astate", CountryCodes.Default, "azip")
            .Value;

        var result = _profile.SetContactAddress("auserid".ToId(), address);

        result.Should().BeSuccess();
        _profile.Address.Line1.Should().Be("aline1");
        _profile.Address.Line2.Should().Be("aline2");
        _profile.Address.Line3.Should().Be("aline3");
        _profile.Address.City.Should().Be("acity");
        _profile.Address.State.Should().Be("astate");
        _profile.Address.CountryCode.Should().Be(CountryCodes.Default);
        _profile.Address.Zip.Should().Be("azip");
        _profile.Events.Last().Should().BeOfType<ContactAddressChanged>();
    }

    [Fact]
    public void WhenSetTimezoneAndNotOwner_ThenReturnsError()
    {
        var timezone = Timezone.Create(Timezones.Default).Value;

        var result = _profile.SetTimezone("anotheruserid".ToId(), timezone);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenSetTimezone_ThenSets()
    {
        var timezone = Timezone.Create(Timezones.Default).Value;

        var result = _profile.SetTimezone("auserid".ToId(), timezone);

        result.Should().BeSuccess();
        _profile.Address.CountryCode.Should().Be(CountryCodes.Default);
        _profile.Events.Last().Should().BeOfType<TimezoneChanged>();
    }

    [Fact]
    public void WhenChangeNameAndNotOwner_ThenReturnsError()
    {
        var name = PersonName.Create("afirstname", "alastname").Value;

        var result = _profile.ChangeName("anotheruserid".ToId(), name);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeName_ThenChanges()
    {
        var name = PersonName.Create("anewfirstname", "anewlastname").Value;

        var result = _profile.ChangeName("auserid".ToId(), name);

        result.Should().BeSuccess();
        _profile.Name.Value.FirstName.Text.Should().Be("anewfirstname");
        _profile.Name.Value.LastName.Value.Text.Should().Be("anewlastname");
        _profile.Events.Last().Should().BeOfType<NameChanged>();
    }

    [Fact]
    public void WhenChangeDisplayNameAndNotOwner_ThenReturnsError()
    {
        var name = PersonDisplayName.Create("adisplayname").Value;

        var result = _profile.ChangeDisplayName("anotheruserid".ToId(), name);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeDisplayName_ThenChanges()
    {
        var name = PersonDisplayName.Create("anewdisplayname").Value;

        var result = _profile.ChangeDisplayName("auserid".ToId(), name);

        result.Should().BeSuccess();
        _profile.DisplayName.Value.Text.Should().Be("anewdisplayname");
        _profile.Events.Last().Should().BeOfType<DisplayNameChanged>();
    }

    [Fact]
    public void WhenChangePhoneNumberAndNotOwner_ThenReturnsError()
    {
        var number = PhoneNumber.Create("+6498876986").Value;

        var result = _profile.ChangePhoneNumber("anotheruserid".ToId(), number);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public void WhenChangePhoneNumber_ThenChanges()
    {
        var number = PhoneNumber.Create("+6498876986").Value;

        var result = _profile.ChangePhoneNumber("auserid".ToId(), number);

        result.Should().BeSuccess();
        _profile.PhoneNumber.Value.Number.Should().Be("+6498876986");
        _profile.Events.Last().Should().BeOfType<PhoneNumberChanged>();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _profile.ChangeAvatarAsync("anotheruserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNotPerson_ThenReturnsError()
    {
#if TESTINGONLY
        _profile.TestingOnly_ChangeType(ProfileType.Machine);
#endif

        var result = await _profile.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("aimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfileRoot_NotAPerson);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNoExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        var result = await _profile.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().BeNull();
        result.Should().BeSuccess();
        _profile.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _profile.Avatar.Value.Url.Should().Be("aurl");
        _profile.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndHasExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        await _profile.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _profile.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _profile.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _profile.Avatar.Value.Url.Should().Be("aurl");
        _profile.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _profile.DeleteAvatarAsync("anotheruserid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfileRoot_NotOwner);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNotPerson_ThenReturnsError()
    {
#if TESTINGONLY
        _profile.TestingOnly_ChangeType(ProfileType.Machine);
#endif

        var result = await _profile.DeleteAvatarAsync("auserid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfileRoot_NotAPerson);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndNoExistingAvatar_ThenReturnsError()
    {
        var result = await _profile.DeleteAvatarAsync("auserid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfileRoot_NoAvatar);
    }

    [Fact]
    public async Task WhenDeleteAvatarAsyncAndHasExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        await _profile.ChangeAvatarAsync("auserid".ToId(),
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _profile.DeleteAvatarAsync("auserid".ToId(), id =>
        {
            imageDeletedId = id;
            return Task.FromResult(Result.Ok);
        });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _profile.Avatar.HasValue.Should().BeFalse();
        _profile.Events.Last().Should().BeOfType<AvatarRemoved>();
    }
}