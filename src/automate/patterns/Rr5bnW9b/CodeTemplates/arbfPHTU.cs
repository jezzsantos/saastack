using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Events.Shared.{{SubdomainName | string.pascalplural}};
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace {{SubdomainName | string.pascalplural}}Domain.UnitTests;

[Trait("Category", "Unit")]
public class {{SubdomainName | string.pascalsingular}}RootSpec
{
    private readonly {{SubdomainName | string.pascalsingular}}Root _{{SubdomainName | string.pascalsingular | string.downcase}};

    public {{SubdomainName | string.pascalsingular}}RootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _{{SubdomainName | string.pascalsingular | string.downcase}} = {{SubdomainName | string.pascalsingular}}Root.Create(recorder.Object, identifierFactory.Object, "anorganizationid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        _{{SubdomainName | string.pascalsingular | string.downcase}}.OrganizationId.Should().Be("anorganizationid".ToId());
    }

    //TODO: add other aggregate tests here
}