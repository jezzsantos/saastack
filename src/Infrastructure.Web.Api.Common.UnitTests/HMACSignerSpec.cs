using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class HMACSignerSpec
{
    [Trait("Category", "Unit")]
    public class GivenARequest
    {
        [Fact]
        public void WhenConstructedWithEmptySecret_ThenThrows()
        {
            FluentActions.Invoking(() => new HMACSigner(new TestHmacRequest(), string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenSignAndRequestIsHollow_ThenReturnsSignature()
        {
            var signer = new HMACSigner(new TestHmacRequest(), "asecret");

            var result = signer.Sign();

            result.Should().Be("sha1=f8dbae1fc1114a368a46f762db4a5ad5417e0e1ea4bc34d7924d166621c45653");
        }

        [Fact]
        public void WhenSignAndRequestIsPopulated_ThenReturnsSignature()
        {
            var signer = new HMACSigner(new TestHmacRequest
            {
                Id = "anid"
            }, "asecret");

            var result = signer.Sign();

            result.Should().Be("sha1=5ac3c7a7378d5f293359720f1bf64cfcbfbf672b61640121a11526638a57a2c5");
        }

        [Fact]
        public void WhenSignAndRequestsAreSame_ThenReturnsSameSignatures()
        {
            var signer1 = new HMACSigner(new TestHmacRequest
            {
                Id = "anid"
            }, "asecret");
            var signer2 = new HMACSigner(new TestHmacRequest
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
            var signer1 = new HMACSigner(new TestHmacRequest
            {
                Id = "anid1"
            }, "asecret");
            var signer2 = new HMACSigner(new TestHmacRequest
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
            var signer = new HMACSigner(new TestHmacRequest(), "asecret");
            var verifier = new HMACVerifier(signer);
            FluentActions.Invoking(() => verifier.Verify(string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenVerifyWithWrongSignature_ThenReturnsFalse()
        {
            var signer = new HMACSigner(new TestHmacRequest(), "asecret");
            var verifier = new HMACVerifier(signer);

            var result = verifier.Verify("awrongsignature");

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenVerifyWithCorrectSignature_ThenReturnsTrue()
        {
            var signer = new HMACSigner(new TestHmacRequest(), "asecret");
            var signature = signer.Sign();
            var verifier = new HMACVerifier(signer);

            var result = verifier.Verify(signature);

            result.Should().BeTrue();
        }
    }

    [Route("/aroute", OperationMethod.Get)]
    public class TestHmacRequest : WebRequest<TestHmacRequest, TestHmacResponse>
    {
        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [UsedImplicitly]
    public class TestHmacResponse : IWebResponse;
}