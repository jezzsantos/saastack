using Common;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class MfaOptionsSpec
{
    private readonly MfaOptions _options;
    private readonly Mock<ITokensService> _tokensService;

    public MfaOptionsSpec()
    {
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(x => x.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _options = MfaOptions.Create(true, true).Value;
    }

    [Fact]
    public void WhenCreate_ThenCreates()
    {
        var result = MfaOptions.Create(true, true);

        result.Should().BeSuccess();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CanBeDisabled.Should().BeTrue();
    }

    [Fact]
    public void WhenEnableWithFalseAndCannotBeDisabled_ThenReturnsError()
    {
        var options = MfaOptions.Create(false, false).Value;

        var result = options.Enable(false);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_Change_CannotBeEnabled);
    }

    [Fact]
    public void WhenEnableWithFalseAndCanBeDisabled_ThenReturnsDisabled()
    {
        var options = MfaOptions.Create(true, true).Value;

        var result = options.Enable(false);

        result.Should().BeSuccess();
        result.Value.IsEnabled.Should().BeFalse();
        result.Value.CanBeDisabled.Should().BeTrue();
    }

    [Fact]
    public void WhenEnableWithTrue_ThenReturnsEnabled()
    {
        var options = MfaOptions.Create(false, true).Value;

        var result = options.Enable(true);

        result.Should().BeSuccess();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CanBeDisabled.Should().BeTrue();
    }

    [Fact]
    public void WhenInitiateAuthenticationAndNotEnabled_ThenReturnsError()
    {
        var options = MfaOptions.Create(false, true).Value;

        var result = options.InitiateAuthentication(_tokensService.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_NotEnabled);
    }

    [Fact]
    public void WhenInitiateAuthentication_ThenInitiates()
    {
        var result = _options.InitiateAuthentication(_tokensService.Object);

        result.Should().BeSuccess();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CanBeDisabled.Should().BeTrue();
        result.Value.AuthenticationTokenExpiresAt.Should()
            .BeNear(DateTime.UtcNow.Add(MfaOptions.DefaultAuthenticationTokenExpiry));
        result.Value.AuthenticationToken.Should().Be("anmfatoken");
    }

    [Fact]
    public void WhenAuthenticateAndNotEnabled_ThenReturnsError()
    {
        var options = MfaOptions.Create(false, true).Value;
        var caller = MfaCaller.Create("acallerid".ToId(), "atoken").Value;

        var result = options.Authenticate(caller);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_NotEnabled);
    }

    [Fact]
    public void WhenAuthenticateByTokenAndAuthenticationNotInitiated_ThenReturnsError()
    {
        var caller = MfaCaller.Create("acallerid".ToId(), "atoken").Value;

        var result = _options.Authenticate(caller);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_AuthenticationNotInitiated);
    }

    [Fact]
    public void WhenAuthenticateByTokenAndTokenMismatch_ThenReturnsError()
    {
        var options = MfaOptions.Create(true, true, "atoken",
            DateTime.UtcNow.AddSeconds(10)).Value;
        var caller = MfaCaller.Create("acallerid".ToId(), "anothertoken").Value;

        var result = options.Authenticate(caller);

        result.Should().BeError(ErrorCode.NotAuthenticated, Resources.MfaOptions_AuthenticationFailed);
    }

    [Fact]
    public void WhenAuthenticateByTokenAndAuthenticationExpired_ThenReturnsError()
    {
        var options = MfaOptions.Create(true, true, "atoken",
            DateTime.UtcNow.AddSeconds(1)).Value;
        var caller = MfaCaller.Create("acallerid".ToId(), "atoken").Value;
#if TESTINGONLY
        options.TestingOnly_ExpireAuthentication();
#endif

        var result = options.Authenticate(caller);

        result.Should().BeError(ErrorCode.NotAuthenticated, Resources.MfaOptions_AuthenticationTokenExpired);
    }

    [Fact]
    public void WhenAuthenticateByToken_ThenAuthenticated()
    {
        var options = MfaOptions.Create(true, true, "atoken",
            DateTime.UtcNow.AddSeconds(10)).Value;
        var caller = MfaCaller.Create("acallerid".ToId(), "atoken").Value;

        var result = options.Authenticate(caller);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenAuthenticateByAuthenticatedUser_ThenAuthenticated()
    {
        var options = MfaOptions.Create(true, true, "atoken",
            DateTime.UtcNow.AddSeconds(10)).Value;
        var caller = MfaCaller.Create("acallerid".ToId(), null).Value;

        var result = options.Authenticate(caller);

        result.Should().BeSuccess();
    }
}