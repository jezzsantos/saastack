using Application.Interfaces;
using {{SubdomainName | string.pascalplural}}Application.Persistence;
using {{SubdomainName | string.pascalplural}}Application.Persistence.ReadModels;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace {{SubdomainName | string.pascalplural}}Application.UnitTests;

[Trait("Category", "Unit")]
public class {{SubdomainName | string.pascalplural}}ApplicationSpec
{
    private readonly {{SubdomainName | string.pascalplural}}Application _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<I{{SubdomainName | string.pascalsingular}}Repository> _repository;

    public {{SubdomainName | string.pascalplural}}ApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _repository = new Mock<I{{SubdomainName | string.pascalsingular}}Repository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<{{SubdomainName | string.pascalsingular}}Root>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(({{SubdomainName | string.pascalsingular}}Root root, CancellationToken _) => root);
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");
        _application = new {{SubdomainName | string.pascalplural}}Application(_recorder.Object, _idFactory.Object,
            _repository.Object);
    }

    //TODO: add tests for other application methods
}