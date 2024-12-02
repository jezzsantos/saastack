using Application.Interfaces;
using Application.Interfaces.Services;
using Common.FeatureFlags;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Interfaces.Clients;
using Moq;
using UnitTesting.Common;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests.Application;

[Trait("Category", "Unit")]
public class FeatureFlagsApplicationSpec
{
    private readonly FeatureFlagsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IServiceClient> _serviceClient;

    public FeatureFlagsApplicationSpec()
    {
        var hostSettings = new Mock<IHostSettings>();
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IServiceClient>();
        _application = new FeatureFlagsApplication(_serviceClient.Object, hostSettings.Object);
    }

    [Fact]
    public async Task WhenGetFeatureFlagForCaller_ThenReturns()
    {
        _serviceClient.Setup(sc => sc.GetAsync(It.IsAny<ICallerContext>(), It.IsAny<GetFeatureFlagForCallerRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetFeatureFlagResponse
            {
                Flag = new FeatureFlag
                {
                    Name = "aname",
                    IsEnabled = true
                }
            });

        var result =
            await _application.GetFeatureFlagForCallerAsync(_caller.Object, "aname", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.IsEnabled.Should().BeTrue();
        _serviceClient.Verify(sc => sc.GetAsync(_caller.Object, It.Is<GetFeatureFlagForCallerRequest>(req =>
            req.Name == "aname"
        ), It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetAllFeatureFlags_ThenReturns()
    {
        _serviceClient.Setup(sc => sc.GetAsync(It.IsAny<ICallerContext>(), It.IsAny<GetAllFeatureFlagsRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAllFeatureFlagsResponse
            {
                Flags = new List<FeatureFlag>
                {
                    new()
                    {
                        Name = "aname",
                        IsEnabled = true
                    }
                }
            });

        var result =
            await _application.GetAllFeatureFlagsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value[0].Name.Should().Be("aname");
        result.Value[0].IsEnabled.Should().BeTrue();
        _serviceClient.Verify(sc => sc.GetAsync(_caller.Object, It.IsAny<GetAllFeatureFlagsRequest>(),
            It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()));
    }
}