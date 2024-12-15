using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Kurrent;

[Trait("Category", "Integration.Persistence")]
[Collection("Kurrent")]
public class KurrentEventStoreSpec : AnyEventStoreBaseSpec
{
    private readonly KurrentSpecSetup _setup;

    public KurrentEventStoreSpec(KurrentSpecSetup setup) : base(setup.EventStore)
    {
        _setup = setup;
    }

    //TODO: all other tests

    //TODO: override the base class tests
}