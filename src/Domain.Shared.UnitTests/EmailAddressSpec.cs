using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class EmailAddressSpec
{
    [Fact]
    public void WhenConstructedAndEmptyEmail_ThenReturnsError()
    {
        var result = EmailAddress.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenConstructedAndInvalidEmail_ThenReturnsError()
    {
        var result = EmailAddress.Create("notanemail");

        result.Should().BeError(ErrorCode.Validation, Resources.EmailAddress_InvalidAddress);
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndPlainUsername_ThenReturnsName()
    {
        var name = EmailAddress.Create("auser@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Auser");
        name.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndMultipleDottedUsername_ThenReturnsName()
    {
        var name = EmailAddress.Create("afirstname.amiddlename.alastname@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Afirstname");
        name.LastName.Value.Text.Should().Be("Alastname");
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndTwoDottedUsername_ThenReturnsName()
    {
        var name = EmailAddress.Create("afirstname.alastname@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Afirstname");
        name.LastName.Value.Text.Should().Be("Alastname");
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndContainsPlusSign_ThenReturnsName()
    {
        var name = EmailAddress.Create("afirstname+anothername@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Afirstname");
        name.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndContainsPlusSignAndNumber_ThenReturnsName()
    {
        var name = EmailAddress.Create("afirstname+9@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Afirstname");
        name.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndGuessedFirstNameNotValid_ThenReturnsNameWithFallbackFirstName()
    {
        var name = EmailAddress.Create("-@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be(Resources.EmailAddress_FallbackGuessedFirstName);
        name.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndGuessedLastNameNotValid_ThenReturnsNameWithNoLastName()
    {
        var name = EmailAddress.Create("afirstname.b@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be("Afirstname");
        name.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenGuessPersonNameFromEmailAndGuessedFirstAndLastNameNotValid_ThenReturnsNameWithNoLastName()
    {
        var name = EmailAddress.Create("1.2@company.com").Value.GuessPersonName();

        name.FirstName.Text.Should().Be(Resources.EmailAddress_FallbackGuessedFirstName);
        name.LastName.Should().BeNone();
    }
}