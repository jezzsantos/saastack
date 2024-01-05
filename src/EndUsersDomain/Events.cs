using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace EndUsersDomain;

public static class Events
{
    public class Created : IDomainEvent
    {
        public static Created Create(Identifier id, UserClassification classification)
        {
            return new Created
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                Classification = classification.ToString(),
                Access = UserAccess.Enabled.ToString(),
                Status = UserStatus.Unregistered.ToString()
            };
        }

        public required string Access { get; set; }

        public required string Classification { get; set; }

        public required string Status { get; set; }

        public required string RootId { get; set; }

        public DateTime OccurredUtc { get; set; }
    }

    public class Registered : IDomainEvent
    {
        public static Registered Create(Identifier id, Optional<EmailAddress> username,
            UserClassification classification,
            UserAccess access, UserStatus status,
            Roles roles,
            FeatureLevels featureLevels)
        {
            return new Registered
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                Username = username.ValueOrDefault!,
                Classification = classification.ToString(),
                Access = access.ToString(),
                Status = status.ToString(),
                Roles = roles.ToList(),
                FeatureLevels = featureLevels.ToList()
            };
        }

        public required string Access { get; set; }

        public required string Classification { get; set; }

        public required List<string> FeatureLevels { get; set; }

        public required List<string> Roles { get; set; }

        public required string Status { get; set; }

        public string? Username { get; set; }

        public required string RootId { get; set; }

        public DateTime OccurredUtc { get; set; }
    }
}