using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

[Trait("Category", "Integration.Persistence")]
[Collection("Kurrent")]
[UsedImplicitly]
public class KurrentEventStoreSpec : AnyEventStoreBaseSpec
{
    private readonly KurrentSpecSetup _setup;

    public KurrentEventStoreSpec(KurrentSpecSetup setup) : base(setup.EventStore)
    {
        _setup = setup;
    }
}