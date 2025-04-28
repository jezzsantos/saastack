using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Interfaces;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Clients;

[Trait("Category", "Unit")]
public class InterHostServiceClientSpec
{
    private readonly InterHostServiceClient _client;

    public InterHostServiceClientSpec()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        _client = new InterHostServiceClient(httpClientFactory.Object, JsonSerializerOptions.Default, "abaseurl",
            "asecret");
    }

    [Fact]
    public void WhenSetAuthorizationAndNoAuthorizationValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();
        var caller =
            Mock.Of<ICallerContext>(cc => cc.Authorization == Optional<ICallerContext.CallerAuthorization>.None);

        InterHostServiceClient.SetAuthorization(message, caller, "asecret");

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAuthorizationAndHMACAuthorization_ThenThrows()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.HMAC, "avalue").ToOptional());

        _client.Invoking(_ => InterHostServiceClient.SetAuthorization(message, caller, "asecret"))
            .Should().Throw<NotSupportedException>()
            .WithMessage(Resources.RequestExtensions_HMACAuthorization_NotSupported);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAuthorizationAndPrivateInterHostAuthorizationAndCallerHasAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost, "avalue")
                .ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should()
            .OnlyContain(hdr => hdr == "sha256=f8dbae1fc1114a368a46f762db4a5ad5417e0e1ea4bc34d7924d166621c45653");
        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == "Bearer avalue");
    }

    [Fact]
    public void WhenSetAuthorizationAndPrivateInterHostAuthorizationAndCallerHasNoAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost,
                    Optional<string>.None)
                .ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should()
            .OnlyContain(hdr => hdr == "sha256=f8dbae1fc1114a368a46f762db4a5ad5417e0e1ea4bc34d7924d166621c45653");
        message.Headers.Contains(HttpConstants.Headers.Authorization).Should().BeFalse();
    }

    [Fact]
    public void WhenSetAuthorizationAndTokenAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost, "avalue")
                .ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == "Bearer avalue");
    }

    [Fact]
    public void WhenSetAuthorizationAndApiKeyAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey, "avalue")
                .ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret");

        var base64Credential = Convert.ToBase64String(Encoding.UTF8.GetBytes("avalue:"));
        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == $"Basic {base64Credential}");
    }
}