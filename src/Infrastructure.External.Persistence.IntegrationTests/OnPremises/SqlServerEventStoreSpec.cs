using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.OnPremises;

[Trait("Category", "Integration.Persistence")]
[Collection("SqlServerStore")]
[UsedImplicitly]
public class SqlServerEventStoreSpec : AnyEventStoreBaseSpec
{
    public SqlServerEventStoreSpec(SqlServerStorageSpecSetup setup) : base(setup.EventStore)
    {
    }
}