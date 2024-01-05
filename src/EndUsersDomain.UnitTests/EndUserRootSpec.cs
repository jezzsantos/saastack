using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using Xunit;
using FeatureLevel = Domain.Shared.FeatureLevel;

namespace EndUsersDomain.UnitTests;

[Trait("Category", "Unit")]
public class EndUserRootSpec
{
    private readonly EndUserRoot _user;

    public EndUserRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _user = EndUserRoot.Create(recorder.Object, identifierFactory.Object, UserClassification.Person).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _user.Access.Should().Be(UserAccess.Enabled);
        _user.Status.Should().Be(UserStatus.Unregistered);
        _user.Classification.Should().Be(UserClassification.Person);
        _user.Roles.HasNone().Should().BeTrue();
        _user.Features.HasNone().Should().BeTrue();
    }

    [Fact]
    public void WhenRegister_ThenRegistered()
    {
        _user.Register(UserClassification.Machine, Roles.Create(PlatformRoles.Standard).Value,
            FeatureLevels.Create(PlatformFeatureLevels.Basic.Name).Value,
            EmailAddress.Create("auser@company.com").Value);

        _user.Access.Should().Be(UserAccess.Enabled);
        _user.Status.Should().Be(UserStatus.Registered);
        _user.Classification.Should().Be(UserClassification.Machine);
        _user.Roles.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value);
        _user.Features.Items.Should().ContainInOrder(FeatureLevel.Create(PlatformFeatureLevels.Basic.Name).Value);
        _user.Events.Last().Should().BeOfType<Events.Registered>();
    }
}