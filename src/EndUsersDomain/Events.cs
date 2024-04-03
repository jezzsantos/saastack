using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Shared;

namespace EndUsersDomain;

public static class Events
{
    public static Created Created(Identifier id, UserClassification classification)
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

    public static GuestInvitationAccepted GuestInvitationAccepted(Identifier id, EmailAddress emailAddress)
    {
        return new GuestInvitationAccepted
        {
            RootId = id,
            OccurredUtc = DateTime.UtcNow,
            AcceptedEmailAddress = emailAddress,
            AcceptedAtUtc = DateTime.UtcNow
        };
    }

    public static GuestInvitationCreated GuestInvitationCreated(Identifier id, string token, EmailAddress invitee,
        Identifier invitedBy)
    {
        return new GuestInvitationCreated
        {
            RootId = id,
            OccurredUtc = DateTime.UtcNow,
            EmailAddress = invitee,
            InvitedById = invitedBy,
            Token = token
        };
    }

    public static MembershipAdded MembershipAdded(Identifier id, Identifier organizationId, bool isDefault, Roles roles,
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

    public static MembershipDefaultChanged MembershipDefaultChanged(Identifier id, Identifier fromMembershipId,
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

    public static MembershipFeatureAssigned MembershipFeatureAssigned(Identifier id, Identifier organizationId,
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

    public static MembershipRoleAssigned MembershipRoleAssigned(Identifier id, Identifier organizationId,
        Identifier membershipId,
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

    public static PlatformFeatureAssigned PlatformFeatureAssigned(Identifier id, Feature feature)
    {
        return new PlatformFeatureAssigned
        {
            RootId = id,
            OccurredUtc = DateTime.UtcNow,
            Feature = feature.Identifier
        };
    }

    public static PlatformRoleAssigned PlatformRoleAssigned(Identifier id, Role role)
    {
        return new PlatformRoleAssigned
        {
            RootId = id,
            OccurredUtc = DateTime.UtcNow,
            Role = role.Identifier
        };
    }

    public static PlatformRoleUnassigned PlatformRoleUnassigned(Identifier id, Role role)
    {
        return new PlatformRoleUnassigned
        {
            RootId = id,
            OccurredUtc = DateTime.UtcNow,
            Role = role.Identifier
        };
    }

    public static Registered Registered(Identifier id, Optional<EmailAddress> username,
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
}