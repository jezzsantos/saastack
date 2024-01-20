using Common;
using Domain.Shared;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class EmailRecipientSpec
{
    [Fact]
    public void WhenCreateAndDisplayNameIsEmpty_ThenReturnsError()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = EmailRecipient.Create(emailAddress, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturnsValue()
    {
        var emailAddress = EmailAddress.Create("auser@company.com").Value;

        var result = EmailRecipient.Create(emailAddress, "adisplayname");

        result.Should().BeSuccess();
        result.Value.EmailAddress.Should().Be(emailAddress);
        result.Value.DisplayName.Should().Be("adisplayname");
    }
}