using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;

namespace EndUsersDomain;

public static class Events
{
    public sealed class Created : IDomainEvent
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

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class Registered : IDomainEvent
    {
        public static Registered Create(Identifier id, Optional<EmailAddress> username,
            UserClassification classification,
            UserAccess access, UserStatus status,
            Roles roles,
            Features features)
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
                Features = features.ToList()
            };
        }

        public required string Access { get; set; }

        public required string Classification { get; set; }

        public required List<string> Features { get; set; }

        public required List<string> Roles { get; set; }

        public required string Status { get; set; }

        public string? Username { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class MembershipAdded : IDomainEvent
    {
        public static MembershipAdded Create(Identifier id, Identifier organizationId, bool isDefault, Roles roles,
            Features features)
        {
            return new MembershipAdded
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                MembershipId = null,
                IsDefault = isDefault,
                OrganizationId = organizationId,
                Roles = roles.ToList(),
                Features = features.ToList()
            };
        }

        public required List<string> Features { get; set; }

        public required bool IsDefault { get; set; }

        public string? MembershipId { get; set; }

        public required string OrganizationId { get; set; }

        public required List<string> Roles { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class MembershipDefaultChanged : IDomainEvent
    {
        public static MembershipDefaultChanged Create(Identifier id, Identifier fromMembershipId,
            Identifier toMembershipId)
        {
            return new MembershipDefaultChanged
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                FromMembershipId = fromMembershipId,
                ToMembershipId = toMembershipId
            };
        }

        public required string FromMembershipId { get; set; }

        public required string ToMembershipId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class MembershipRoleAssigned : IDomainEvent
    {
        public static MembershipRoleAssigned Create(Identifier id, Identifier organizationId, Identifier membershipId,
            Role role)
        {
            return new MembershipRoleAssigned
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                OrganizationId = organizationId,
                MembershipId = membershipId,
                Role = role.Identifier
            };
        }

        public required string MembershipId { get; set; }

        public required string OrganizationId { get; set; }

        public required string Role { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class MembershipFeatureAssigned : IDomainEvent
    {
        public static MembershipFeatureAssigned Create(Identifier id, Identifier organizationId,
            Identifier membershipId, Feature feature)
        {
            return new MembershipFeatureAssigned
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                OrganizationId = organizationId,
                MembershipId = membershipId,
                Feature = feature.Identifier
            };
        }

        public required string Feature { get; set; }

        public required string MembershipId { get; set; }

        public required string OrganizationId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class PlatformRoleAssigned : IDomainEvent
    {
        public static PlatformRoleAssigned Create(Identifier id, Role role)
        {
            return new PlatformRoleAssigned
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                Role = role.Identifier
            };
        }

        public required string Role { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public sealed class PlatformFeatureAssigned : IDomainEvent
    {
        public static PlatformFeatureAssigned Create(Identifier id, Feature feature)
        {
            return new PlatformFeatureAssigned
            {
                RootId = id,
                OccurredUtc = DateTime.UtcNow,
                Feature = feature.Identifier
            };
        }

        public required string Feature { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }
}