using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.OnPremises;

[Trait("Category", "Integration.Persistence")]
[Collection("SqlServerStore")]
[UsedImplicitly]
public class SqlServerDataStoreSpec : AnyDataStoreBaseSpec
{
    public SqlServerDataStoreSpec(SqlServerStorageSpecSetup setup) : base(setup.DataStore)
    {
    }
}
