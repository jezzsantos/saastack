using System.Text;
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
    [Fact]
    public void WhenSetAuthorizationAndNoAuthorizationValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();
        var caller =
            Mock.Of<ICallerContext>(cc => cc.Authorization == Optional<ICallerContext.CallerAuthorization>.None);

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAuthorizationAndHMACAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.HMAC, "avalue").ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should()
            .OnlyContain(hdr => hdr == "sha256=2314af1b8cf6d257dfd692c335b91d4a5b6dbc8376abdee194b6032bee17aae7");
    }

    [Fact]
    public void WhenSetAuthorizationAndPrivateInterHostAuthorizationAndCallerHasAuthorization_ThenAuthorizes()
    {
        var message = new HttpRequestMessage();
        var caller = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.PrivateInterHost, "avalue")
                .ToOptional());

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

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

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

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

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

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

        InterHostServiceClient.SetAuthorization(message, caller, "asecret", "asecret");

        var base64Credential = Convert.ToBase64String(Encoding.UTF8.GetBytes("avalue:"));
        message.Headers.GetValues(HttpConstants.Headers.Authorization).Should()
            .OnlyContain(hdr => hdr == $"Basic {base64Credential}");
    }
}