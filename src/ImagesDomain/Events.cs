using Domain.Common.ValueObjects;
using Domain.Events.Shared.Images;

namespace ImagesDomain;

public static class Events
{
    public static AttributesChanged AttributesChanged(Identifier id, long size)
    {
        return new AttributesChanged(id)
        {
            Size = size
        };
    }

    public static Created Created(Identifier id, Identifier createById, string contentType)
    {
        return new Created(id)
        {
            CreatedById = createById,
            ContentType = contentType
        };
    }

    public static Deleted Deleted(Identifier id, Identifier deletedById)
    {
        return new Deleted(id, deletedById);
    }

    public static DetailsChanged DetailsChanged(Identifier id, string? description, string? filename)
    {
        return new DetailsChanged(id)
        {
            Description = description,
            Filename = filename
        };
    }
}