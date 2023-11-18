using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces.Entities;
using JetBrains.Annotations;

namespace Application.Persistence.Common.UnitTests;

[UsedImplicitly]
public class TestEntity : EntityBase
{
    public TestEntity(IRecorder recorder, IIdentifierFactory idFactory)
        : base(recorder, idFactory, null)
    {
    }

    public string APropertyName { get; private set; } = null!;

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        return Result.Ok;
    }
}