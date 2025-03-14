using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

/// <summary>
///     Test phone numbers from: https://se.au/resources/test-phone-numbers/
/// </summary>
[Trait("Category", "Unit")]
public class PhoneNumbersSpec
{
    [Fact]
    public void WhenIsValidInternationalAndNull_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndEmpty_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational(string.Empty);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndNotANumber_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational("notanumber");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndNotEnoughChars_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational("1234");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndLocalNz_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational("098876986");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndLocalUsa_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational("5019262756");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndLocalUsaToll_ThenReturnsFalse()
    {
        var result = PhoneNumbers.IsValidInternational("08004444444");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidInternationalAndInternationalNz_ThenReturnsTrue()
    {
        var result = PhoneNumbers.IsValidInternational("+6498876986");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidInternationalAndInternationalUsa_ThenReturnsTrue()
    {
        var result = PhoneNumbers.IsValidInternational("+15019262756");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidInternationalAndInternationalUsaToll_ThenReturnsTrue()
    {
        var result = PhoneNumbers.IsValidInternational("+18004444444");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenTryToInternationalAWithNull_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational(null, null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithEmpty_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational(string.Empty, null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithNotANumber_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("anotanumber", null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithNotEnoughChars_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("1234", null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithLocalNzAndNoHint_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("098876986", null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithLocalNzAndHint_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("098876986", "NZ", out var number);

        result.Should().BeTrue();
        number.Should().Be("+64 9 887 6986");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalNzAndNoHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("+6498876986", null, out var number);

        result.Should().BeTrue();
        number.Should().Be("+64 9 887 6986");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalPartialNzAndNoHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("6498876986", null, out var number);

        result.Should().BeTrue();
        number.Should().Be("+64 9 887 6986");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalPartialNzAndHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("6498876986", "NZ", out var number);

        result.Should().BeTrue();
        number.Should().Be("+64 9 887 6986");
    }

    [Fact]
    public void WhenTryToInternationalAWithLocalUsaAndNoHint_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("5019262756", null, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenTryToInternationalAWithLocalUsaAndHint_ThenReturnsFalse()
    {
        var result = PhoneNumbers.TryToInternational("5019262756", "US", out var number);

        result.Should().BeTrue();
        number.Should().Be("+1 501-926-2756");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalUsaAndNoHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("+15019262756", null, out var number);

        result.Should().BeTrue();
        number.Should().Be("+1 501-926-2756");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalPartialUsaAndNoHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("15019262756", null, out var number);

        result.Should().BeTrue();
        number.Should().Be("+1 501-926-2756");
    }

    [Fact]
    public void WhenTryToInternationalAWithInternationalPartialUsaAndAndHint_ThenReturnsTrue()
    {
        var result = PhoneNumbers.TryToInternational("15019262756", "US", out var number);

        result.Should().BeTrue();
        number.Should().Be("+1 501-926-2756");
    }
}