using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

[Trait("Category", "Integration.Persistence")]
[Collection("Kurrent")]
[UsedImplicitly]
public class KurrentEventStoreSpec : AnyEventStoreBaseSpec
{
    public KurrentEventStoreSpec(KurrentSpecSetup setup) : base(setup.EventStore)
    {
    }
}