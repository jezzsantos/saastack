using Application.Persistence.Interfaces;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

[EntityName("testblobstore")]
public class TestDataBlobStoreEntity : IHasIdentity, IQueryableEntity
{
    private static int _instanceCounter;

    public TestDataBlobStoreEntity()
    {
        Id = $"anid{++_instanceCounter:00000}";
    }

    public string BlobName { get; set; }

    public Optional<string> ContentType { get; set; }

    public byte[] Data { get; set; } = null!;

    public Optional<string> Id { get; set; }
}