using System.Text;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Moq;
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
}