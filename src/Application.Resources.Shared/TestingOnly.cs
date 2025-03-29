using Application.Interfaces.Resources;

#if TESTINGONLY
namespace Application.Resources.Shared;

public class TestResource : IIdentifiableResource
{
    public string? AProperty { get; set; }

    public required string Id { get; set; }
}
#endif