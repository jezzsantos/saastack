using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Persistence.Interfaces;
using Moq;
using QueryAny;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

[Trait("Category", "Unit")]
public class ProjectionCheckpointRepositorySpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly ProjectionCheckpointRepository _repository;
    private readonly Mock<IDataStore> _store;

    public ProjectionCheckpointRepositorySpec()
    {
        var recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var domainFactory = new Mock<IDomainFactory>();
        domainFactory.Setup(df => df.RehydrateValueObject(typeof(Identifier), It.IsAny<string>()))
            .Returns((Type _, string value) => Identifier.Create(value));
        _store = new Mock<IDataStore>();
        _store.Setup(repo => repo.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>());
        _repository = new ProjectionCheckpointRepository(recorder.Object, _idFactory.Object,
            domainFactory.Object, _store.Object);
    }

    [Fact]
    public async Task WhenLoadCheckpointAndNotExists_ThenReturnsStartingVersion()
    {
        var result = await _repository.LoadCheckpointAsync("astreamname", CancellationToken.None);

        result.Value.Should().Be(ProjectionCheckpointRepository.StartingCheckpointVersion);
    }

    [Fact]
    public async Task WhenLoadCheckpointAndExists_ThenReturnsPosition()
    {
        _store.Setup(repo => repo.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>
            {
                QueryEntity.FromType(new Checkpoint { Position = 10 })
            });

        var result = await _repository.LoadCheckpointAsync("astreamname", CancellationToken.None);

        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task WhenSaveCheckpointAndNotExists_ThenSavesNewPosition()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anewid".ToId());

        await _repository.SaveCheckpointAsync("astreamname", 10, CancellationToken.None);

        _store.Verify(cs => cs.AddAsync(GetContainerName(), It.Is<CommandEntity>(
            entity =>
                entity.Id == "anewid"
                && (int)entity.Properties[nameof(Checkpoint.Position)] == 10
                && entity.Properties[nameof(Checkpoint.StreamName)].ToString() == "astreamname"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSaveCheckpointAndExists_ThenSavesExistingPosition()
    {
        var existing = new Checkpoint { Id = "anid", Position = 1 };
        _store.Setup(repo => repo.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>
            {
                QueryEntity.FromType(existing)
            });

        await _repository.SaveCheckpointAsync("astreamname", 10, CancellationToken.None);

        _store.Verify(cs => cs.ReplaceAsync(GetContainerName(), "anid".ToId(), It.Is<CommandEntity>(
            entity =>
                entity.Id == "anid"
                && (int)entity.Properties[nameof(Checkpoint.Position)] == 10
        ), It.IsAny<CancellationToken>()));
    }

    private static string GetContainerName()
    {
        return typeof(Checkpoint).GetEntityNameSafe();
    }
}