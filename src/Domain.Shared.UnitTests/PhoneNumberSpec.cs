using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class PhoneNumberSpec
{
    [Fact]
    public void WhenConstructWithInvalidNumber_ThenReturnsError()
    {
        var result = PhoneNumber.Create("aninvalidnumber");

        result.Should().BeError(ErrorCode.Validation, Resources.PhoneNumber_InvalidPhoneNumber);
    }

    [Fact]
    public void WhenConstructWithNationalNumber_ThenReturnsError()
    {
        var result = PhoneNumber.Create("098876986");

        result.Should().BeError(ErrorCode.Validation, Resources.PhoneNumber_InvalidPhoneNumber);
    }

    [Fact]
    public void WhenConstructWithInternationalNumber_ThenReturnsNumber()
    {
        var result = PhoneNumber.Create("+6498876986");

        result.Should().BeSuccess();
        result.Value.Number.Should().Be("+6498876986");
    }
}