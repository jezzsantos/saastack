using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using SigningsApplication.Persistence;
using SigningsDomain;
using UnitTesting.Common;
using Xunit;

namespace SigningsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SigningsApplicationSec
{
    private readonly SigningsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISigningRepository> _repository;

    public SigningsApplicationSec()
    {
        _caller = new Mock<ICallerContext>();
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _repository = new Mock<ISigningRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SigningRequestRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SigningRequestRoot root, CancellationToken _) => root);

        _application = new SigningsApplication(_recorder.Object, _identifierFactory.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCreateDraftAsync_ThenCreatesDraft()
    {
        var result = await _application.CreateDraftAsync(_caller.Object, "organizationId", new List<Signee>(),
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.OrganizationId.Should().Be("organizationId");
        _repository.Verify(rep => rep.SaveAsync(It.Is<SigningRequestRoot>(root =>
            root.OrganizationId == "organizationId"
        ), It.IsAny<CancellationToken>()));
    }
}