using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Events;

public static class Global
{
    public class StreamDeleted : ITombstoneEvent
    {
        public static StreamDeleted Create(Identifier id, Identifier deletedById)
        {
            return new StreamDeleted
            {
                RootId = id,
                DeletedById = deletedById,
                IsTombstone = true,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string DeletedById { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }

        public required bool IsTombstone { get; set; }
    }
}