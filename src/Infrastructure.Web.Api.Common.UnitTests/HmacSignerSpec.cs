using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class HmacSignerSpec
{
    [Trait("Category", "Unit")]
    public class GivenARequest
    {
        [Fact]
        public void WhenConstructedWithEmptySecret_ThenThrows()
        {
            FluentActions.Invoking(() => new HmacSigner(new TestHmacRequest(), string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenSignAndRequestIsHollow_ThenReturnsSignature()
        {
            var signer = new HmacSigner(new TestHmacRequest(), "asecret");

            var result = signer.Sign();

            result.Should().Be("sha1=a0619224b43c173b0bb02e163534fccf7e16060e2347a0e514416451a0923cdb");
        }

        [Fact]
        public void WhenSignAndRequestIsPopulated_ThenReturnsSignature()
        {
            var signer = new HmacSigner(new TestHmacRequest
            {
                Id = "anid"
            }, "asecret");

            var result = signer.Sign();

            result.Should().Be("sha1=110e558fb8c4f34f199808e0fd020a835af949f4dea2a898d6185ebcb10d4d92");
        }

        [Fact]
        public void WhenSignAndRequestsAreSame_ThenReturnsSameSignatures()
        {
            var signer1 = new HmacSigner(new TestHmacRequest
            {
                Id = "anid"
            }, "asecret");
            var signer2 = new HmacSigner(new TestHmacRequest
            {
                Id = "anid"
            }, "asecret");

            var result1 = signer1.Sign();
            var result2 = signer2.Sign();

            result1.Should().Be(result2);
        }

        [Fact]
        public void WhenSignAndRequestsAreDifferent_ThenReturnsDifferentSignatures()
        {
            var signer1 = new HmacSigner(new TestHmacRequest
            {
                Id = "anid1"
            }, "asecret");
            var signer2 = new HmacSigner(new TestHmacRequest
            {
                Id = "anid2"
            }, "asecret");

            var result1 = signer1.Sign();
            var result2 = signer2.Sign();

            result1.Should().NotBe(result2);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenASigner
    {
        [Fact]
        public void WhenVerifyWithEmptySignature_ThenThrows()
        {
            var signer = new HmacSigner(new TestHmacRequest(), "asecret");
            var verifier = new HmacVerifier(signer);
            FluentActions.Invoking(() => verifier.Verify(string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenVerifyWithWrongSignature_ThenReturnsFalse()
        {
            var signer = new HmacSigner(new TestHmacRequest(), "asecret");
            var verifier = new HmacVerifier(signer);

            var result = verifier.Verify("awrongsignature");

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenVerifyWithCorrectSignature_ThenReturnsTrue()
        {
            var signer = new HmacSigner(new TestHmacRequest(), "asecret");
            var signature = signer.Sign();
            var verifier = new HmacVerifier(signer);

            var result = verifier.Verify(signature);

            result.Should().BeTrue();
        }
    }

    [Route("/aroute", ServiceOperation.Get)]
    public class TestHmacRequest : IWebRequest<TestHmacResponse>
    {
        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [UsedImplicitly]
    public class TestHmacResponse : IWebResponse
    {
    }
}