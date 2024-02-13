using Application.Interfaces;
using Common;
using Common.FeatureFlags;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace AncillaryApplication.UnitTests;

[Trait("Category", "Unit")]
public class FeatureFlagsApplicationSpec
{
    private readonly FeatureFlagsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IFeatureFlags> _featuresService;

    public FeatureFlagsApplicationSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.IsAuthenticated).Returns(true);
        _caller.Setup(cc => cc.CallerId).Returns("acallerid");
        _caller.Setup(cc => cc.TenantId).Returns("atenantid");
        _featuresService = new Mock<IFeatureFlags>();
        _featuresService.Setup(fs => fs.GetFlagAsync(It.IsAny<Flag>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureFlag
            {
                Name = "aname",
                IsEnabled = true
            });
        _application = new FeatureFlagsApplication(recorder.Object, _featuresService.Object);
    }

    [Fact]
    public async Task WhenGetFeatureFlag_ThenReturns()
    {
        var result =
            await _application.GetFeatureFlagAsync(_caller.Object, "aname", null, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.IsEnabled.Should().BeTrue();
        _featuresService.Verify(fs => fs.GetFlagAsync(It.Is<Flag>(flag => flag.Name == "aname"), Optional<string>.None,
            "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetFeatureFlagForCaller_ThenReturns()
    {
        var result =
            await _application.GetFeatureFlagForCallerAsync(_caller.Object, "aname", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.IsEnabled.Should().BeTrue();
        _featuresService.Verify(fs => fs.GetFlagAsync(It.Is<Flag>(flag =>
            flag.Name == "aname"
        ), "atenantid", "acallerid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetAllFeatureFlags_ThenReturns()
    {
        _featuresService.Setup(fs => fs.GetAllFlagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>
            {
                new()
                {
                    Name = "aname",
                    IsEnabled = true
                }
            });

        var result =
            await _application.GetAllFeatureFlagsAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value[0].Name.Should().Be("aname");
        result.Value[0].IsEnabled.Should().BeTrue();
        _featuresService.Verify(fs => fs.GetAllFlagsAsync(It.IsAny<CancellationToken>()));
    }
}