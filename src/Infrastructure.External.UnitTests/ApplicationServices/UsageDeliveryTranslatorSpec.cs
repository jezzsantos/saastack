using Application.Interfaces;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.External.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class UsageDeliveryTranslatorSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly UsageDeliveryTranslator _translator;

    public UsageDeliveryTranslatorSpec()
    {
        _caller = new Mock<ICallerContext>();
        _translator = new UsageDeliveryTranslator();
    }

    [Fact]
    public void WhenGetBrowserPropertiesAndNoProperties_ThenReturnsComponents()
    {
        var result = _translator.GetBrowserProperties(new Dictionary<string, string>());

        result.Referrer.Should().BeNull();
        result.Url.Should().BeNull();
        result.IpAddress.Should().BeNull();
    }

    [Fact]
    public void WhenGetBrowserPropertiesAndBackupProperties_ThenReturnsComponents()
    {
        var result = _translator.GetBrowserProperties(new Dictionary<string, string>
        {
            { UsageConstants.Properties.ReferredBy, "areferrer" },
            { UsageConstants.Properties.IpAddress, "anipaddress" }
        });

        result.Referrer.Should().Be("areferrer");
        result.Url.Should().BeNull();
        result.IpAddress.Should().Be("anipaddress");
    }

    [Fact]
    public void WhenGetBrowserPropertiesAndHasAllProperties_ThenReturnsComponents()
    {
        var result = _translator.GetBrowserProperties(new Dictionary<string, string>
        {
            { UsageDeliveryTranslator.BrowserReferrer, "areferrer" },
            { UsageConstants.Properties.Path, "aurl" },
            { UsageDeliveryTranslator.BrowserIp, "anipaddress" }
        });

        result.Referrer.Should().Be("areferrer");
        result.Url.Should().Be("aurl");
        result.IpAddress.Should().Be("anipaddress");
    }

    [Fact]
    public void WhenGetUserAgentPropertiesAndNoValue_ThenReturnsComponents()
    {
        var result = _translator.GetUserAgentProperties(null);

        result.Browser.Should().BeNull();
        result.BrowserVersion.Should().BeNull();
        result.OperatingSystem.Should().BeNull();
        result.Device.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserAgentPropertiesAndUnknownValue_ThenReturnsComponents()
    {
        var result = _translator.GetUserAgentProperties("anunknownuseragent");

        result.Browser.Should().Be("Other");
        result.BrowserVersion.Should().Be("..");
        result.OperatingSystem.Should().Be("Other");
        result.Device.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserAgentPropertiesAndKnownValue_ThenReturnsComponents()
    {
        var result =
            _translator.GetUserAgentProperties(
                "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:47.0) Gecko/20100101 Firefox/47.0");

        result.Browser.Should().Be("Firefox");
        result.BrowserVersion.Should().Be("47.0.");
        result.OperatingSystem.Should().Be("Windows");
        result.Device.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserPropertiesForAnyEventAndNoAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties("anevent", null);

        result.CreatedAt.Should().BeNull();
        result.Name.Should().BeNull();
        result.EmailAddress.Should().BeNull();
        result.Timezone.Should().BeNull();
        result.CountryCode.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserPropertiesForPersonRegistrationCreatedAndNoAdditionalData_ThenReturns()
    {
        var result =
            _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated, null);

        result.CreatedAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Name.Should().BeNull();
        result.EmailAddress.Should().BeNull();
        result.Timezone.Should().BeNull();
        result.CountryCode.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserPropertiesForPersonMachineRegisteredAndNoAdditionalData_ThenReturns()
    {
        var result =
            _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.MachineRegistered, null);

        result.CreatedAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Name.Should().BeNull();
        result.EmailAddress.Should().BeNull();
        result.Timezone.Should().BeNull();
        result.CountryCode.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserPropertiesForMembershipAddedAndNoAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.MembershipAdded, null);

        result.CreatedAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Name.Should().BeNull();
        result.EmailAddress.Should().BeNull();
        result.Timezone.Should().BeNull();
        result.CountryCode.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void WhenGetUserPropertiesForUserLoginWithAllAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                { UsageConstants.Properties.Timezone, "atimezone" },
                { UsageConstants.Properties.CountryCode, "acountrycode" },
                { UsageConstants.Properties.AvatarUrl, "anavatarurl" }
            });

        result.CreatedAt.Should().BeNull();
        result.Name.Should().Be("aname");
        result.EmailAddress.Should().Be("anemailaddress");
        result.Timezone.Should().Be("atimezone");
        result.CountryCode.Should().Be("acountrycode");
        result.AvatarUrl.Should().Be("anavatarurl");
    }

    [Fact]
    public void WhenGetUserPropertiesForPersonRegistrationCreatedWithAllAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated, new Dictionary<string, string>
            {
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                { UsageConstants.Properties.Timezone, "atimezone" },
                { UsageConstants.Properties.CountryCode, "acountrycode" },
                { UsageConstants.Properties.AvatarUrl, "anavatarurl" }
            });

        result.CreatedAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Name.Should().Be("aname");
        result.EmailAddress.Should().Be("anemailaddress");
        result.Timezone.Should().Be("atimezone");
        result.CountryCode.Should().Be("acountrycode");
        result.AvatarUrl.Should().Be("anavatarurl");
    }

    [Fact]
    public void WhenGetUserPropertiesForMachineRegisteredWithAllAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.MachineRegistered,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                { UsageConstants.Properties.Timezone, "atimezone" },
                { UsageConstants.Properties.CountryCode, "acountrycode" },
                { UsageConstants.Properties.AvatarUrl, "anavatarurl" }
            });

        result.CreatedAt.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Name.Should().Be("aname");
        result.EmailAddress.Should().Be("anemailaddress");
        result.Timezone.Should().Be("atimezone");
        result.CountryCode.Should().Be("acountrycode");
        result.AvatarUrl.Should().Be("anavatarurl");
    }

    [Fact]
    public void WhenGetUserPropertiesForUserProfileChangedWithAllAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                { UsageConstants.Properties.Timezone, "atimezone" },
                { UsageConstants.Properties.CountryCode, "acountrycode" },
                { UsageConstants.Properties.AvatarUrl, "anavatarurl" }
            });

        result.CreatedAt.Should().BeNull();
        result.Name.Should().Be("aname");
        result.EmailAddress.Should().Be("anemailaddress");
        result.Timezone.Should().Be("atimezone");
        result.CountryCode.Should().Be("acountrycode");
        result.AvatarUrl.Should().Be("anavatarurl");
    }

    [Fact]
    public void WhenGetUserPropertiesForMembershipChangedWithAllAdditionalData_ThenReturns()
    {
        var result = _translator.GetUserProperties(UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                { UsageConstants.Properties.Timezone, "atimezone" },
                { UsageConstants.Properties.CountryCode, "acountrycode" },
                { UsageConstants.Properties.AvatarUrl, "anavatarurl" }
            });

        result.CreatedAt.Should().BeNull();
        result.Name.Should().Be("aname");
        result.EmailAddress.Should().Be("anemailaddress");
        result.Timezone.Should().Be("atimezone");
        result.CountryCode.Should().Be("acountrycode");
        result.AvatarUrl.Should().Be("anavatarurl");
    }

    [Fact]
    public void WhenIsUserIdentifiableEventAndNotStarted_ThenThrows()
    {
        _translator.Invoking(x => x.IsUserIdentifiableEvent())
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(UsageDeliveryTranslator.StartTranslation)));
    }

    [Fact]
    public void WhenIsUserIdentifiableEventAndNoAdditionalData_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname", null, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForAnyEventAndAdditionalData_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname", new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForUserLoginAndHasNoUserIdOverride_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForUserLoginAndHasUserIdOverride_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.UserIdOverride, "auserid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForPersonRegistrationCreatedAndHasNoId_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMachineRegisteredAndHasNoId_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MachineRegistered,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForPersonRegistrationCreatedAndHasId_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMachineRegisteredAndHasId_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MachineRegistered,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForUserProfileChangedAndHasNoIdNameOrEmail_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForUserProfileChangedAndHasIdNameAndEmail_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" },
                { UsageConstants.Properties.Name, "aname" },
                { UsageConstants.Properties.EmailAddress, "anemailaddress" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForOrganizationCreatedAndHasNoId_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForOrganizationChangedAndHasNoId_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForOrganizationCreatedAndHasId_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForOrganizationChangedAndHasId_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMembershipAddedAndHasNoIdOrTenantOverride_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMembershipChangedAndHasNoIdOrTenantOverride_ThenReturnsFalse()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
            new Dictionary<string, string>(), false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMembershipAddedAndHasIdAndTenantOverride_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" },
                { UsageConstants.Properties.TenantIdOverride, "anid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsUserIdentifiableEventForMembershipChangedAndHasIdAndTenantOverride_ThenReturnsTrue()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "anid" },
                { UsageConstants.Properties.TenantIdOverride, "anid" }
            }, false);

        var result = _translator.IsUserIdentifiableEvent();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenPreparePropertiesAndNotStarted_ThenThrows()
    {
        _translator.Invoking(x => x.PrepareProperties(false, s => s))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(UsageDeliveryTranslator.StartTranslation)));
    }

    [Fact]
    public void WhenPreparePropertiesAndNoAdditionalData_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            null, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(1);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
    }

    [Fact]
    public void WhenPreparePropertiesForAnyEventWithAnyProperties_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(3);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
    }

    [Fact]
    public void WhenPreparePropertiesForAnyEventWithAnyIgnoredProperties_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { UsageConstants.Properties.UserIdOverride, "auseridoverride" },
                { UsageConstants.Properties.TenantIdOverride, "atenantidoverride" },
                { UsageConstants.Properties.DefaultOrganizationId, "adefaultorganizationid" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(3);
        result.Should().Contain(UsageConstants.Properties.TenantId, "atenantidoverride");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
    }

    [Fact]
    public void WhenPreparePropertiesForUserLoginWithNoUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid", UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(3);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
    }

    [Fact]
    public void WhenPreparePropertiesForPersonRegistrationCreatedWithNoUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(3);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
    }

    [Fact]
    public void WhenPreparePropertiesForUserProfileChangedWithNoUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(3);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
    }

    [Fact]
    public void WhenPreparePropertiesForUserLoginWithUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid", UsageConstants.Events.UsageScenarios.Generic.UserLogin,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { UsageConstants.Properties.UserIdOverride, "auseridoverride" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(4);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
        result.Should().Contain(UsageConstants.Properties.Id, "auseridoverride");
    }

    [Fact]
    public void WhenPreparePropertiesForPersonRegistrationCreatedWithUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { UsageConstants.Properties.UserIdOverride, "auseridoverride" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(4);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
        result.Should().Contain(UsageConstants.Properties.Id, "auseridoverride");
    }

    [Fact]
    public void WhenPreparePropertiesForUserProfileChangedWithUserIdOverride_ThenReturnsProperties()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>
            {
                { "aproperty1", "avalue1" },
                { UsageConstants.Properties.UserIdOverride, "auseridoverride" },
                { "aproperty2", "avalue2" }
            }, false);

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(4);
        result.Should().Contain(UsageConstants.Properties.TenantId, "platform");
        result.Should().Contain("aproperty1", "avalue1");
        result.Should().Contain("aproperty2", "avalue2");
        result.Should().Contain(UsageConstants.Properties.Id, "auseridoverride");
    }

    [Fact]
    public void WhenRecalculateTenantIdAndNotStarted_ThenThrows()
    {
        _translator.Invoking(x => x.RecalculateTenantId("atenantid"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(UsageDeliveryTranslator.StartTranslation)));
    }

    [Fact]
    public void WhenRecalculateTenantId_ThenOverridesTenantId()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>(), false);

        _translator.RecalculateTenantId("atenantid");

        var result = _translator.PrepareProperties(false, s => s);

        result.Count.Should().Be(1);
        result.Should().Contain(UsageConstants.Properties.TenantId, "atenantid");
    }

    [Fact]
    public void WhenStartTranslationWithNoUserIdOverride_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>(), false);

        _translator.UserId.Should().Be("aforid");
        _translator.TenantId.Should().Be("platform");
    }

    [Fact]
    public void WhenStartTranslationWithUserIdOverride_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, "aforid",
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.UserIdOverride, "auseridoverride" }
            }, false);

        _translator.UserId.Should().Be("auseridoverride");
        _translator.TenantId.Should().Be("platform");
    }

    [Fact]
    public void WhenStartTranslationWithAnonymousUserId_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, CallerConstants.AnonymousUserId,
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>(), false);

        _translator.UserId.Should().Be("anonymous");
        _translator.TenantId.Should().Be("platform");
    }

    [Fact]
    public void WhenStartTranslationWithAnonymousUserIdAndUsingTenantedUserIds_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, CallerConstants.AnonymousUserId,
            UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            new Dictionary<string, string>(), true);

        _translator.UserId.Should().Be("anonymous@platform");
        _translator.TenantId.Should().Be("platform");
    }

    [Fact]
    public void WhenStartTranslationWithNoTenantId_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>(), false);

        _translator.UserId.Should().Be("aforid");
        _translator.TenantId.Should().Be("platform");
    }

    [Fact]
    public void WhenStartTranslationWithTenantId_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.TenantId, "atenantid" }
            }, false);

        _translator.UserId.Should().Be("aforid");
        _translator.TenantId.Should().Be("atenantid");
    }

    [Fact]
    public void WhenStartTranslationWithTenantIdOverride_ThenStarted()
    {
        _translator.StartTranslation(_caller.Object, "aforid", "aneventname",
            new Dictionary<string, string>
            {
                { UsageConstants.Properties.TenantId, "atenantid" },
                { UsageConstants.Properties.TenantIdOverride, "atenantidoverride" }
            }, false);

        _translator.UserId.Should().Be("aforid");
        _translator.TenantId.Should().Be("atenantidoverride");
    }
}