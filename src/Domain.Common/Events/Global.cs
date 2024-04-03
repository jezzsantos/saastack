using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using JetBrains.Annotations;

namespace Domain.Common.Events;

public static class Global
{
    /// <summary>
    ///     Defines an event raised when a stream is deleted
    /// </summary>
    public class StreamDeleted : DomainEvent, ITombstoneEvent
    {
        public static StreamDeleted Create(Identifier id, Identifier deletedById)
        {
            return new StreamDeleted(id)
            {
                DeletedById = deletedById,
                IsTombstone = true
            };
        }

        public StreamDeleted(Identifier id) : base(id)
        {
        }

        [UsedImplicitly]
        public StreamDeleted()
        {
        }

        public required string DeletedById { get; set; }

        public required bool IsTombstone { get; set; }
    }
}