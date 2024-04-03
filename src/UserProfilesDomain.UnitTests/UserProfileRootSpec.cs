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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
    }

    [Fact]
    public void WhenSetEmailAddressAndNotAPerson_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;
#if TESTINGONLY
        _profile.TestingOnly_ChangeType(ProfileType.Machine);
#endif

        var result = _profile.SetEmailAddress("auserid".ToId(), emailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UserProfilesDomain_NotAPerson);
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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
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

        result.Should().BeError(ErrorCode.RoleViolation, Resources.UserProfilesDomain_NotOwner);
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
}