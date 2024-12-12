using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Moq;
using QueryAny;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

[Trait("Category", "Unit")]
public class ProjectionCheckpointRepositorySpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly ProjectionCheckpointRepository _repository;
    private readonly Mock<ISnapshottingStore<Checkpoint>> _store;

    public ProjectionCheckpointRepositorySpec()
    {
        var recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _store = new Mock<ISnapshottingStore<Checkpoint>>();
        _store.Setup(store => store.QueryAsync(It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<Checkpoint>(new List<Checkpoint>()));

        _store.Setup(store => store.UpsertAsync(It.IsAny<Checkpoint>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Checkpoint entity, bool _, CancellationToken _) => entity);
        _repository = new ProjectionCheckpointRepository(recorder.Object, _idFactory.Object, _store.Object);
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
        _store.Setup(store => store.QueryAsync(It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<Checkpoint>(new List<Checkpoint>
            {
                new()
                    { Position = 10 }
            }));

        var result = await _repository.LoadCheckpointAsync("astreamname", CancellationToken.None);

        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task WhenSaveCheckpointAndNotExists_ThenSavesNewPosition()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anewid".ToId());

        await _repository.SaveCheckpointAsync("astreamname", 10, CancellationToken.None);

        _store.Verify(cs => cs.UpsertAsync(It.Is<Checkpoint>(
            entity =>
                entity.Id == "anewid"
                && entity.Position == 10
                && entity.StreamName == "astreamname"
        ), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSaveCheckpointAndExists_ThenSavesExistingPosition()
    {
        var existing = new Checkpoint { Id = "anid", Position = 1 };
        _store.Setup(store => store.QueryAsync(It.IsAny<QueryClause<Checkpoint>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<Checkpoint>(new List<Checkpoint>
            {
                existing
            }));

        await _repository.SaveCheckpointAsync("astreamname", 10, CancellationToken.None);

        _store.Verify(cs => cs.UpsertAsync(It.Is<Checkpoint>(
            entity =>
                entity.Id == "anid"
                && entity.Position == 10
        ), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
    }
}