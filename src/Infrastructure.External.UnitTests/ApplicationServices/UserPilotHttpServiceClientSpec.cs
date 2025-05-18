using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.External.ApplicationServices;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.External.UnitTests.ApplicationServices;

[UsedImplicitly]
public class UserPilotHttpServiceClientSpec
{
    [Trait("Category", "Unit")]
    public class GivenANonIdentifiableEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenANonIdentifiableEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNotTenantedByAnonymous_ThenTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, CallerConstants.AnonymousUserId, "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "anonymous@platform", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndNotTenantedByUser_ThenTracks()
        {
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndTenantedByAnonymous_ThenTracks()
        {
            _caller.Setup(cc => cc.TenantId)
                .Returns("atenantid");
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, CallerConstants.AnonymousUserId, "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "anonymous@atenantid", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "atenantid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndTenantedByUser_ThenTracks()
        {
            _caller.Setup(cc => cc.TenantId)
                .Returns("atenantid");
            var result =
                await _serviceClient.DeliverAsync(_caller.Object, "aforid", "aneventname", null,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@atenantid", "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "atenantid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndAdditionalPropertiesByAnonymous_ThenTracks()
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "anoverriddenuserid@platform",
                "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic["aname1"] == "avalue1"
                    && dic["aname2"] == datum.ToUnixSeconds().ToString()
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndAdditionalPropertiesByUser_ThenTracks()
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "anoverriddenuserid@platform",
                "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic["aname1"] == "avalue1"
                    && dic["aname2"] == datum.ToUnixSeconds().ToString()
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "anoverriddenuserid@platform",
                "aneventname",
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 10
                    && dic[UserPilotConstants.MetadataProperties.ReferredBy] == "areferrer"
                    && dic[UserPilotConstants.MetadataProperties.Url] == "aurl"
                    && dic[UserPilotConstants.MetadataProperties.IpAddress] == "anipaddress"
                    && dic[UserPilotConstants.MetadataProperties.Browser] == "Other"
                    && dic[UserPilotConstants.MetadataProperties.BrowserVersion] == ".."
                    && dic[UserPilotConstants.MetadataProperties.OperatingSystem] == "Other"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic["aname1"] == "avalue1"
                    && dic["aname2"] == datum.ToUnixSeconds().ToString()
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheUserLoginEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheUserLoginEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.UserLogin,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.AuthProvider] == "aprovider"
                    && dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenThePersonRegistrationCreatedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenThePersonRegistrationCreatedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndId_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now)
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverride_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now)
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndIdOverrideAndNameAndEmailAddress_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 3
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now)
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheUserProfileChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheUserProfileChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 4
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Classification] == "aclassification"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                    && dic[UsageConstants.Properties.Classification] == "aclassification"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@platform",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@platform",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                    && dic[UsageConstants.Properties.Classification] == "aclassification"
                ), It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@adefaultorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.Id] == "auserid"
                    && dic[UsageConstants.Properties.TenantId] == "adefaultorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                    && dic[UsageConstants.Properties.Classification] == "aclassification"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheOrganizationCreatedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheOrganizationCreatedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationId_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now, TimeSpan.FromMinutes(1))
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndName_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 3
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                        && dic[UserPilotConstants.MetadataProperties.CompanyNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now, TimeSpan.FromMinutes(1))
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndOrganizationIdAndNameAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 3
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                        && dic[UserPilotConstants.MetadataProperties.CompanyNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now, TimeSpan.FromMinutes(1))
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheOrganizationChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheOrganizationChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                        && dic[UserPilotConstants.MetadataProperties.CompanyNamePropertyName] == "aname"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.Ownership] == "anownership"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                        && dic[UserPilotConstants.MetadataProperties.CompanyNamePropertyName] == "aname"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "anorganizationid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.Ownership] == "anownership"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheMembershipAddedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheMembershipAddedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantOverrideId_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now)
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenDeliverAsyncAndMembershipIdAndTenantIdOverrideAndUserIdOverride_ThenIdentifiesAndTracks()
        {
            var now = DateTime.UtcNow.ToNearestSecond();
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CreatedAtPropertyName].ToLong().FromUnixTimestamp()
                            .IsNear(now)
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipAdded,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTheMembershipChangedEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IUserPilotClient> _client;
        private readonly UserPilotHttpServiceClient _serviceClient;

        public GivenTheMembershipChangedEvent()
        {
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _caller.Setup(cc => cc.CallId)
                .Returns("acallid");
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IUserPilotClient>();

            _serviceClient = new UserPilotHttpServiceClient(recorder.Object, _client.Object);
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 2
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic["aname"] == "avalue"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@platform",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "platform"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "aforid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 0
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 3
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
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
                c => c.IdentifyUserAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                    It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 2
                        && dic[UserPilotConstants.MetadataProperties.UserNamePropertyName] == "aname"
                        && dic[UserPilotConstants.MetadataProperties.UserEmailAddressPropertyName] == "anemailaddress"
                    ), It.Is<Dictionary<string, string>>(dic =>
                        dic.Count == 1
                        && dic[UserPilotConstants.MetadataProperties.CompanyIdPropertyName] == "anorganizationid"
                    ),
                    It.IsAny<CancellationToken>()));
            _client.Verify(c => c.TrackEventAsync(It.IsAny<ICallContext>(), "auserid@anorganizationid",
                UsageConstants.Events.UsageScenarios.Generic.MembershipChanged,
                It.Is<Dictionary<string, string>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.Id] == "amembershipid"
                    && dic[UsageConstants.Properties.Name] == "aname"
                    && dic[UsageConstants.Properties.EmailAddress] == "anemailaddress"
                    && dic[UsageConstants.Properties.TenantId] == "anorganizationid"
                    && dic[UsageConstants.Properties.CallId] == "acallid"
                ), It.IsAny<CancellationToken>()));
        }
    }
}