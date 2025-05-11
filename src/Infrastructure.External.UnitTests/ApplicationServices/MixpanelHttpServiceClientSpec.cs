using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
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
    public class GivenANonProfilingEvent
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IMixpanelClient> _client;
        private readonly MixpanelHttpServiceClient _serviceClient;

        public GivenANonProfilingEvent()
        {
            var recorder = new Mock<IRecorder>();
            _client = new Mock<IMixpanelClient>();
            _caller = new Mock<ICallerContext>();

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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anonymous@platform", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "anonymous@platform"
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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid@platform", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "aforid@platform"
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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anonymous@atenantid", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "anonymous@atenantid"
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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "aforid@atenantid", "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 8
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "aforid@atenantid"
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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anoverriddenuserid@platform",
                "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "anoverriddenuserid@platform"
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
            _client.Verify(c => c.ImportAsync(It.IsAny<ICallContext>(), "anoverriddenuserid@platform",
                "aneventname",
                It.Is<MixpanelEventProperties>(dic =>
                    dic.Count == 10
                    && (long?)dic["time"] > 0
                    && (string?)dic["distinct_id"] == "anoverriddenuserid@platform"
                    && (string?)dic[UsageConstants.Properties.TenantId] == "platform"
                    && (string?)dic["aname1"] == "avalue1"
                    && (string?)dic["aname2"] == datum.ToIso8601()
                ), It.IsAny<CancellationToken>()));
        }

        //TODO: any tests that are specific to MixPanel now where it is not identifiable
    }
}