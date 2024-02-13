using System.Text;
using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HttpRequestExtensionsSpec
{
    [Fact]
    public async Task WhenVerifyHMACSignatureAsyncAndWrongSignature_ThenReturnsFalse()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(hr => hr.Body)
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes("abody")));

        var result =
            await httpRequest.Object.VerifyHMACSignatureAsync("awrongsignature", "asecret", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenVerifyHMACSignatureAsyncAndEmptyJson_ThenReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes(RequestExtensions.EmptyRequestJson);
        var signature = new HMACSigner(body, "asecret").Sign();
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(hr => hr.Body)
            .Returns(new MemoryStream(body));

        var result = await httpRequest.Object.VerifyHMACSignatureAsync(signature, "asecret", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenVerifyHMACSignatureAsyncAndCorrectSignature_ThenReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("abody");
        var signature = new HMACSigner(body, "asecret").Sign();
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(hr => hr.Body)
            .Returns(new MemoryStream(body));

        var result = await httpRequest.Object.VerifyHMACSignatureAsync(signature, "asecret", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenSetRequestIdAndCallIdEmpty_ThenNoSetHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == string.Empty);
        var message = new HttpRequestMessage();

        message.SetRequestId(context);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetRequestIdAndHeaderAlreadySet_ThenNoSetHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == "acallid");
        var message = new HttpRequestMessage
        {
            Headers = { { HttpHeaders.RequestId, "arequestid" } }
        };

        message.SetRequestId(context);

        message.Headers.GetValues(HttpHeaders.RequestId).Should().OnlyContain(hdr => hdr == "arequestid");
    }

    [Fact]
    public void WhenSetRequestIdAndHeaderNotSet_ThenSetsHeader()
    {
        var context = Mock.Of<ICallContext>(cc => cc.CallId == "acallid");
        var message = new HttpRequestMessage();

        message.SetRequestId(context);

        message.Headers.GetValues(HttpHeaders.RequestId).Should().OnlyContain(hdr => hdr == "acallid");
    }

    [Fact]
    public void WhenSetHmacAuthWithEmptyRequest_ThenSetsSignatureHeader()
    {
        var request = new TestEmptyRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpHeaders.HMACSignature).Should().OnlyContain(hdr =>
            hdr == "sha1=f8dbae1fc1114a368a46f762db4a5ad5417e0e1ea4bc34d7924d166621c45653");
    }

    [Fact]
    public void WhenSetHmacAuthWithPopulatedRequest_ThenSetsSignatureHeader()
    {
        var request = new TestPopulatedRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpHeaders.HMACSignature).Should().OnlyContain(hdr =>
            hdr == "sha1=efab63816eb3c3ab799dfd64e717865116f7aa1547177293ec6598fe3cc8e3de");
    }

    [Fact]
    public void WhenSetJWTBearerTokenAndNoTokenValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();

        message.SetJWTBearerToken(string.Empty);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetJWTBearerToken_ThenSetsBearerAuthorization()
    {
        var message = new HttpRequestMessage();

        message.SetJWTBearerToken("atoken");

        message.Headers.GetValues(HttpHeaders.Authorization).Should().OnlyContain(hdr => hdr == "Bearer atoken");
    }

    [Fact]
    public void WhenSetAPIKeyAndNoKeyValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();

        message.SetAPIKey(string.Empty);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAPIKey_ThenSetsBasicAuthorization()
    {
        var message = new HttpRequestMessage();

        message.SetAPIKey("anapikey");

        var base64Credential = Convert.ToBase64String(Encoding.UTF8.GetBytes("anapikey:"));
        message.Headers.GetValues(HttpHeaders.Authorization).Should()
            .OnlyContain(hdr => hdr == $"Basic {base64Credential}");
    }

    [Fact]
    public void WhenSetAuthorizationAndNoAuthorizationValue_ThenDoesNothing()
    {
        var message = new HttpRequestMessage();
        var context =
            Mock.Of<ICallerContext>(cc => cc.Authorization == Optional.None<ICallerContext.CallerAuthorization>());

        message.SetAuthorization(context);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAuthorizationAndHMACAuthorization_ThenThrows()
    {
        var message = new HttpRequestMessage();
        var context = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.HMAC, "avalue").ToOptional());

        message.Invoking(x => x.SetAuthorization(context))
            .Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.HttpRequestExtensions_HMACAuthorizationNotSupported);

        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetAuthorizationAndTokenAuthorization_ThenThrows()
    {
        var message = new HttpRequestMessage();
        var context = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token, "avalue").ToOptional());

        message.SetAuthorization(context);

        message.Headers.GetValues(HttpHeaders.Authorization).Should().OnlyContain(hdr => hdr == "Bearer avalue");
    }

    [Fact]
    public void WhenSetAuthorizationAndApiKeyAuthorization_ThenThrows()
    {
        var message = new HttpRequestMessage();
        var context = Mock.Of<ICallerContext>(cc =>
            cc.Authorization
            == new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.APIKey, "avalue")
                .ToOptional());

        message.SetAuthorization(context);

        var base64Credential = Convert.ToBase64String(Encoding.UTF8.GetBytes("avalue:"));
        message.Headers.GetValues(HttpHeaders.Authorization).Should()
            .OnlyContain(hdr => hdr == $"Basic {base64Credential}");
    }

    [Fact]
    public void WhenRewindBodAndCanSeek_ThenRewindsStream()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("avalue"));
        stream.Seek(0, SeekOrigin.End);
        var httpRequest = Mock.Of<HttpRequest>(req => req.Body == stream);

        httpRequest.RewindBody();

        stream.Position.Should().Be(0);
    }

    [Fact]
    public void WhenGetTokenAuthAndNoAuthorizationHeader_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndNoAuthorizationHeaderValues_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, new StringValues(new string[] { }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndAuthorizationHeaderValuesContainsNoBearer_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, new StringValues(new[] { "avalue1", "avalue2" }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetTokenAuthAndAuthorizationHeaderValuesContainsBearer_ThenReturnsToken()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, new StringValues(new[] { "avalue1", "Bearer avalue2" }) }
        });

        var result = httpRequest.Object.GetTokenAuth();

        result.Should().BeSome("avalue2");
    }

    [Fact]
    public void WhenGetBasicAuthAndNoAuthorizationHeader_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderNotBasic_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, "Bearer atoken" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButEmpty_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButOnlyUsername_ThenReturnsOnlyUsername()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ausername:"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeSome("ausername");
        result.Password.Should().BeNone();
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasicButOnlyPassword_ThenReturnsOnlyPassword()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeNone();
        result.Password.Should().BeSome("apassword");
    }

    [Fact]
    public void WhenGetBasicAuthAndAuthorizationHeaderBasic_ThenReturnsFullCredentials()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("ausername:apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });

        var result = httpRequest.Object.GetBasicAuth();

        result.Username.Should().BeSome("ausername");
        result.Password.Should().BeSome("apassword");
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndNoQueryParameterAndNoBasicAuthUsername_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderButNoCredentials_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderButOnlyPassword_ThenReturnsNone()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(":apassword"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndAuthorizationHeaderWithBasicUsernameOnly_ThenReturnsAPIKey()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("anapikey:"));
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.Authorization, $"Basic {credentials}" }
        });
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection());

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeSome("anapikey");
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndParameterEmpty_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection
        (new Dictionary<string, StringValues>
        {
            { HttpQueryParams.APIKey, "" }
        }));

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAPIKeyAuthAndParameter_ThenReturnsAPIKey()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());
        httpRequest.Setup(req => req.Query).Returns(new QueryCollection
        (new Dictionary<string, StringValues>
        {
            { HttpQueryParams.APIKey, "anapikey" }
        }));

        var result = httpRequest.Object.GetAPIKeyAuth();

        result.Should().BeSome("anapikey");
    }

    [Fact]
    public void WhenGetHMACAuthAndNoSignature_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary());

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetHMACAuthAndSignatureHasEmptyValue_ThenReturnsNone()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.HMACSignature, "" }
        });

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetHMACAuthAndSignatureHasValue_ThenReturnsSignature()
    {
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(req => req.Headers).Returns(new HeaderDictionary
        {
            { HttpHeaders.HMACSignature, "asignature" }
        });

        var result = httpRequest.Object.GetHMACAuth();

        result.Should().BeSome("asignature");
    }
}

public class TestPopulatedRequest : IWebRequest
{
    public string AProperty => "avalue";
}

public class TestEmptyRequest : IWebRequest
{
}