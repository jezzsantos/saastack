using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class PersonNameSpec
{
    [Fact]
    public void WhenConstructWithFirstName_ThenReturnsFirstName()
    {
        var result = PersonName.Create("afirstname", Optional<string>.None).Value;

        result.FirstName.Text.Should().Be("afirstname");
        result.LastName.Should().BeNone();
    }

    [Fact]
    public void WhenConstructWithFirstNameAndLastName_ThenReturnsBothNames()
    {
        var result = PersonName.Create("afirstname", "alastname").Value;

        result.FirstName.Text.Should().Be("afirstname");
        result.LastName.Value.Text.Should().Be("alastname");
    }

    [Fact]
    public void WhenFullNameWithFirstName_ThenReturnsFullName()
    {
        var result = PersonName.Create("afirstname", Optional<string>.None).Value.FullName;

        result.Text.Should().Be("afirstname");
    }

    [Fact]
    public void WhenFullNameWithFirstNameAndLastName_ThenReturnsFullName()
    {
        var result = PersonName.Create("afirstname", "alastname").Value.FullName;

        result.Text.Should().Be("afirstname alastname");
    }
}