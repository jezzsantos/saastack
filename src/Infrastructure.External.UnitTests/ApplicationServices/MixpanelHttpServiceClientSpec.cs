using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.External.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.External.UnitTests.ApplicationServices;

[UsedImplicitly]
public class MixpanelHttpServiceClientSpec
{
    [Trait("Category", "Unit")]
    public class GivenANonIdentifiableEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenANonIdentifiableEvent()
        {
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");

            _serviceClient =
                new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNotTenantedByAnonymous_ThenImports()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, CallerConstants.AnonymousUserId, "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anonymous", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == string.Empty
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNotTenantedByUser_ThenImports()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndTenantedByAnonymous_ThenImports()
        {
            _caller.Setup(cc => cc.TenantId)
                .Returns("atenantid");
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, CallerConstants.AnonymousUserId, "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anonymous", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == string.Empty
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "atenantid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndTenantedByUser_ThenImports()
        {
            _caller.Setup(cc => cc.TenantId)
                .Returns("atenantid");
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "atenantid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndAdditionalPropertiesByAnonymous_ThenImports()
        {
            var datum = DateTime.UtcNow;

            var result =
                await _serviceClient.DeliverAsync(_caller.Object, CallerConstants.AnonymousUserId, "aneventname",
                    new Dictionary<string, string>
                    {
                        { "aname1", "avalue1" },
                        { "aname2", datum.ToIso8601() },
                        { UsageConstants.Properties.UserIdOverride, "anoverriddenuserid" }
                    }, CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anoverriddenuserid",
                "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "anoverriddenuserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic["aname1"] == "avalue1"
                    && (string?)dic["aname2"] == datum.ToIso8601()
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndAdditionalPropertiesByUser_ThenImports()
        {
            var datum = DateTime.UtcNow;

            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname",
                    new Dictionary<string, string>
                    {
                        { "aname1", "avalue1" },
                        { "aname2", datum.ToIso8601() },
                        { UsageConstants.Properties.UserIdOverride, "anoverriddenuserid" }
                    }, CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anoverriddenuserid",
                "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "anoverriddenuserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname1"] == "avalue1"
                    && (string?)dic["aname2"] == datum.ToIso8601()
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndAdditionalBrowserPropertiesByUser_ThenImports()
        {
            var datum = DateTime.UtcNow;

            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname",
                    new Dictionary<string, string>
                    {
                        { "aname1", "avalue1" },
                        { "aname2", datum.ToIso8601() },
                        { UsageConstants.Properties.ReferredBy, "areferrer" },
                        { UsageConstants.Properties.Path, "aurl" },
                        { UsageConstants.Properties.IpAddress, "anipaddress" },
                        { UsageConstants.Properties.CallId, "acallid" },
                        { UsageConstants.Properties.UserAgent, "auseragent" },
                        { UsageConstants.Properties.UserIdOverride, "anoverriddenuserid" }
                    }, CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(), It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anoverriddenuserid",
                "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 13
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "anoverriddenuserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.ReferredBy] == "areferrer"
                    && (string?)dic[MixpanelConstants.MetadataProperties.Url] == "aurl"
                    && (string?)dic[MixpanelConstants.MetadataProperties.IpAddress] == "anipaddress"
                    && (string?)dic[MixpanelConstants.MetadataProperties.Browser] == "Other"
                    && (string?)dic[MixpanelConstants.MetadataProperties.BrowserVersion] == ".."
                    && (string?)dic[MixpanelConstants.MetadataProperties.OperatingSystem] == "Other"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname1"] == "avalue1"
                    && (string?)dic["aname2"] == datum.ToIso8601()
                ), It.IsAny<CancellationToken>()));
        }

#if TESTINGONLY
        [Fact]
        public void WhenTestingOnly_SanitizeInsertIdWithBadCharacters_ThenRemoves()
        {
            var result =
                MixpanelHttpServiceClient.TestingOnly_SanitizeInsertId(
                    "!@#$%^&*()_+01234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");

            result.Should().Be("01234567890abcdefghijklmnopqrstuvwxy");
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenTestingOnly_SanitizeInsertIdWithTooManyCharacters_ThenTruncates()
        {
            var result =
                MixpanelHttpServiceClient.TestingOnly_SanitizeInsertId(
                    "1234567890123456789012345678901234567890");

            result.Should().Be("123456789012345678901234567890123456");
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenTestingOnly_SanitizeDistinctIdWithDisallowedValue_ThenReturnsEmpty()
        {
            var result =
                MixpanelHttpServiceClient.TestingOnly_SanitizeDistinctId("00000000-0000-0000-0000-000000000000");

            result.Should().Be(string.Empty);
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenTestingOnly_SanitizeDistinctIdWithAllowedValue_ThenReturns()
        {
            var result =
                MixpanelHttpServiceClient.TestingOnly_SanitizeDistinctId("avalue");

            result.Should().Be("avalue");
        }
#endif
    }

    [Trait("Category", "Unit")]
    public class GivenTheUserLoginEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheUserLoginEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserLogin, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoIdOverride_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideButNoDefaultOrganizationId_ThenIdentifiesAndTracksPlatformUser()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.AuthProvider, "aprovider" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.AuthProvider, "aprovider" },
                        { UsageConstants.Properties.DefaultOrganizationId, "adefaultorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideNameAndEmailAddress_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.AuthProvider, "aprovider" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                        { UsageConstants.Properties.DefaultOrganizationId, "adefaultorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == "aname"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName]
                        == "anemailaddress"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 12
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenThePersonRegistrationCreatedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenThePersonRegistrationCreatedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoIdOverride_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndId_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "auserid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideAndNameAndEmailAddress_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "auserid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.EmailAddress, "anemailaddress" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == "aname"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName]
                        == "anemailaddress"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheUserProfileChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheUserProfileChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoIdOverride_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideButNoNameOrEmailAddress_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "aprofileid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.Classification, "aclassification" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.Classification] == "aclassification"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenDeliverAsyncAndIdOverrideAndNameAndEmailAddressButNoDefaultOrganizationId_ThenIdentifiesAndTracksPlatformUser()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "aprofileid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                        { UsageConstants.Properties.Classification, "aclassification" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == "aname"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName]
                        == "anemailaddress"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 12
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.Classification] == "aclassification"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideAndNameAndEmailAddress_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "aprofileid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                        { UsageConstants.Properties.Classification, "aclassification" },
                        { UsageConstants.Properties.DefaultOrganizationId, "adefaultorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == "aname"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName]
                        == "anemailaddress"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 12
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "auserid"
                    && (string?)dic[UsageConstants.Properties.Classification] == "aclassification"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheOrganizationCreatedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheOrganizationCreatedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoOrganizationId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationId_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndName_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.Ownership, "anownership" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndNameAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.Ownership, "anownership" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheOrganizationChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheOrganizationChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoOrganizationId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationId_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndName_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.Ownership, "anownership" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndNameAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "anorganizationid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.Ownership, "anownership" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheMembershipAddedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheMembershipAddedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipAdded, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoMembershipId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdButNoTenantOverrideId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantOverrideId_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" },
                        { UsageConstants.Properties.TenantIdOverride, "anorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantIdOverrideAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" },
                        { UsageConstants.Properties.TenantIdOverride, "anorganizationid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (string?)dic[UsageConstants.Properties.Id] == "amembershipid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheMembershipChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenTheMembershipChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();

            _serviceClient = new MixpanelHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNoProperties_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged, null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncButNoMembershipId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    new Dictionary<string, string>
                    {
                        { "aname", "avalue" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic["aname"] == "avalue"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdButNoTenantOverrideId_ThenJustTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<MixpanelProfileProperties>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantOverrideId_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" },
                        { UsageConstants.Properties.TenantIdOverride, "anorganizationid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "aforid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "aforid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantIdOverrideAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" },
                        { UsageConstants.Properties.TenantIdOverride, "anorganizationid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 9
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task
            WhenDeliverAsyncAndMembershipIdAndTenantIdOverrideAndUserIdOverrideAndNameAndEmail_ThenIdentifiesAndTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid",
                    UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                    new Dictionary<string, string>
                    {
                        { UsageConstants.Properties.Id, "amembershipid" },
                        { UsageConstants.Properties.Name, "aname" },
                        { UsageConstants.Properties.EmailAddress, "anemailaddress" },
                        { UsageConstants.Properties.TenantIdOverride, "anorganizationid" },
                        { UsageConstants.Properties.UserIdOverride, "auserid" }
                    },
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.SetProfileAsync(It.IsAny<ICallContext>(), "auserid",
                    It.Is<MixpanelProfileProperties>(dic =>
                        dic.Count == 6
                        && (bool?)dic[MixpanelConstants.MetadataProperties.UnsubscribedPropertyName] == true
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileNamePropertyName] == "aname"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileEmailAddressPropertyName]
                        == "anemailaddress"
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileTimezonePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileCountryCodePropertyName] == null
                        && (string?)dic[MixpanelConstants.MetadataProperties.ProfileAvatarPropertyName] == null
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "auserid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 11
                    && (long?)dic[MixpanelConstants.MetadataProperties.Time] > 0
                    && (string?)dic[MixpanelConstants.MetadataProperties.DistinctId] == "auserid"
                    && (string?)dic[MixpanelConstants.MetadataProperties.InsertId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && (string?)dic[UsageConstants.Properties.CallId] == "acallid"
                    && (string?)dic[UsageConstants.Properties.ResourceId] == "amembershipid"
                    && (string?)dic[UsageConstants.Properties.Name] == "aname"
                    && (string?)dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }
}