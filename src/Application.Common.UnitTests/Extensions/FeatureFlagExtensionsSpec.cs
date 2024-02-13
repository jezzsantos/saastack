using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.FeatureFlags;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Application.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class FeatureFlagExtensionsSpec
{
    private readonly Mock<ICallerContext> _caller = new();
    private readonly Mock<IFeatureFlags> _featureFlags = new();

    public FeatureFlagExtensionsSpec()
    {
        _caller.Setup(x => x.IsAuthenticated)
            .Returns(true);
        _caller.Setup(x => x.CallerId)
            .Returns("auserid");
        _caller.Setup(x => x.TenantId)
            .Returns("atenantid");
#if TESTINGONLY
        _featureFlags.Setup(ff => ff.IsEnabled(Flag.TestingOnly))
            .Returns(true);
        _featureFlags.Setup(ff => ff.IsEnabled(Flag.TestingOnly, It.IsAny<string>()))
            .Returns(true);
        _featureFlags.Setup(ff => ff.IsEnabled(Flag.TestingOnly, It.IsAny<Optional<string>>(), It.IsAny<string>()))
            .Returns(true);
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncAndNotAuthenticated_ThenGetsFlagForAllUsers()
    {
#if TESTINGONLY
        _caller.Setup(x => x.IsAuthenticated)
            .Returns(false);

        var result = await _featureFlags.Object.GetFlagAsync(Flag.TestingOnly, _caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _featureFlags.Verify(x => x.GetFlagAsync(Flag.TestingOnly, Optional<string>.None, Optional<string>.None,
            It.IsAny<CancellationToken>()));
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncAndAuthenticatedButNoTenant_ThenGetsFlagForUser()
    {
#if TESTINGONLY
        _caller.Setup(x => x.TenantId)
            .Returns((string?)null);

        var result = await _featureFlags.Object.GetFlagAsync(Flag.TestingOnly, _caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _featureFlags.Verify(x =>
            x.GetFlagAsync(Flag.TestingOnly, Optional<string>.None, "auserid", It.IsAny<CancellationToken>()));
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncAndAuthenticated_ThenGetsFlagForUser()
    {
#if TESTINGONLY
        var result = await _featureFlags.Object.GetFlagAsync(Flag.TestingOnly, _caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _featureFlags.Verify(x =>
            x.GetFlagAsync(Flag.TestingOnly, "atenantid", "auserid", It.IsAny<CancellationToken>()));
#endif
    }

    [Fact]
    public void WhenIsEnabledAndNotAuthenticated_ThenIsEnabled()
    {
#if TESTINGONLY
        _caller.Setup(x => x.IsAuthenticated)
            .Returns(false);

        var result = _featureFlags.Object.IsEnabled(Flag.TestingOnly, _caller.Object);

        result.Should().BeTrue();
        _featureFlags.Verify(x => x.IsEnabled(Flag.TestingOnly));
#endif
    }

    [Fact]
    public void WhenIsEnabledAndAuthenticatedButNoTenant_ThenIsEnabled()
    {
#if TESTINGONLY
        _caller.Setup(x => x.TenantId)
            .Returns((string?)null);

        var result = _featureFlags.Object.IsEnabled(Flag.TestingOnly, _caller.Object);

        result.Should().BeTrue();
        _featureFlags.Verify(x => x.IsEnabled(Flag.TestingOnly, "auserid"));
#endif
    }

    [Fact]
    public void WhenIsEnabledAndAuthenticatedAndTenant_ThenIsEnabled()
    {
#if TESTINGONLY
        var result = _featureFlags.Object.IsEnabled(Flag.TestingOnly, _caller.Object);

        result.Should().BeTrue();
        _featureFlags.Verify(x => x.IsEnabled(Flag.TestingOnly, "atenantid", "auserid"));
#endif
    }
}