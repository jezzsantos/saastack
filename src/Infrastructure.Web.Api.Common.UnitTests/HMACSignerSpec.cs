using FluentAssertions;
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
        public void WhenGenerateKey_ThenReturnsRandomKey()
        {
#if TESTINGONLY
            var result = HMACSigner.GenerateKey();

            result.Should().NotBeNullOrEmpty();
#endif
        }

        [Fact]
        public void WhenConstructedWithEmptySecret_ThenThrows()
        {
            FluentActions.Invoking(() => new HMACSigner("avalue", string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenSignAndEmptyData_ThenReturnsSignature()
        {
            var signer = new HMACSigner(string.Empty, "asecret");

            var result = signer.Sign();

            result.Should().Be("sha256=a0619224b43c173b0bb02e163534fccf7e16060e2347a0e514416451a0923cdb");
        }

        [Fact]
        public void WhenSignAndHasData_ThenReturnsSignature()
        {
            var signer = new HMACSigner("avalue", "asecret");

            var result = signer.Sign();

            result.Should().Be("sha256=2254d39b44e099ef8e4c2d732fe1980ca9b2ca2bbd5c02233b67019401979c99");
        }

        [Fact]
        public void WhenSignAndDataIsSame_ThenReturnsSameSignatures()
        {
            var signer1 = new HMACSigner("avalue", "asecret");
            var signer2 = new HMACSigner("avalue", "asecret");

            var result1 = signer1.Sign();
            var result2 = signer2.Sign();

            result1.Should().Be(result2);
        }

        [Fact]
        public void WhenSignAndDataIsDifferent_ThenReturnsDifferentSignatures()
        {
            var signer1 = new HMACSigner("avalue1", "asecret");
            var signer2 = new HMACSigner("avalue2", "asecret");

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
            var signer = new HMACSigner("", "asecret");
            var verifier = new HMACVerifier(signer);

            FluentActions.Invoking(() => verifier.Verify(string.Empty))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WhenVerifyWithWrongSignature_ThenReturnsFalse()
        {
            var signer = new HMACSigner("avalue", "asecret");
            var verifier = new HMACVerifier(signer);

            var result = verifier.Verify("awrongsignature");

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenVerifyWithCorrectSignature_ThenReturnsTrue()
        {
            var signer = new HMACSigner("avalue", "asecret");
            var signature = signer.Sign();
            var verifier = new HMACVerifier(signer);

            var result = verifier.Verify(signature);

            result.Should().BeTrue();
        }
    }
}