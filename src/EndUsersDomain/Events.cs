using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using Created = Domain.Events.Shared.EndUsers.Created;

namespace EndUsersDomain;

public static class Events
{
    public static Created Created(Identifier id, UserClassification classification, DatacenterLocation hostRegion)
    {
        return new Created(id)
        {
            Classification = classification,
            Access = UserAccess.Enabled,
            Status = UserStatus.Unregistered,
            HostRegion = hostRegion.Code
        };
    }

    public static DefaultMembershipChanged DefaultMembershipChanged(Identifier id,
        Optional<Identifier> fromMembershipId, Identifier toMembershipId, Identifier toOrganizationId, Roles roles,
        Features features)
    {
        return new DefaultMembershipChanged(id)
        {
            FromMembershipId = fromMembershipId.ValueOrDefault!,
            ToMembershipId = toMembershipId,
            ToOrganizationId = toOrganizationId,
            Roles = roles.Denormalize(),
            Features = features.Denormalize()
        };
    }

    public static GuestInvitationAccepted GuestInvitationAccepted(Identifier id, EmailAddress emailAddress)
    {
        return new GuestInvitationAccepted(id)
        {
            AcceptedEmailAddress = emailAddress,
            AcceptedAtUtc = DateTime.UtcNow
        };
    }

    public static GuestInvitationCreated GuestInvitationCreated(Identifier id, string token, EmailAddress invitee,
        Identifier invitedBy)
    {
        return new GuestInvitationCreated(id)
        {
            EmailAddress = invitee,
            InvitedById = invitedBy,
            Token = token
        };
    }

    public static MembershipAdded MembershipAdded(Identifier id, Identifier organizationId,
        OrganizationOwnership ownership, bool isDefault, Roles roles,
        Features features)
    {
        return new MembershipAdded(id)
        {
            MembershipId = null,
            IsDefault = isDefault,
            OrganizationId = organizationId,
            Roles = roles.Denormalize(),
            Features = features.Denormalize(),
            Ownership = ownership
        };
    }

    public static MembershipFeatureAssigned MembershipFeatureAssigned(Identifier id, Identifier organizationId,
        Identifier membershipId, Feature feature)
    {
        return new MembershipFeatureAssigned(id)
        {
            OrganizationId = organizationId,
            MembershipId = membershipId,
            Feature = feature.Identifier
        };
    }

    public static MembershipFeaturesReset MembershipFeaturesReset(Identifier id, Identifier assignerId,
        Identifier organizationId,
        Identifier membershipId, Features features)
    {
        return new MembershipFeaturesReset(id)
        {
            AssignedById = assignerId,
            OrganizationId = organizationId,
            MembershipId = membershipId,
            Features = features.Items.Select(x => x.Identifier).ToList()
        };
    }

    public static MembershipFeatureUnassigned MembershipFeatureUnassigned(Identifier id, Identifier organizationId,
        Identifier membershipId, Feature feature)
    {
        return new MembershipFeatureUnassigned(id)
        {
            OrganizationId = organizationId,
            MembershipId = membershipId,
            Feature = feature.Identifier
        };
    }

    public static MembershipRemoved MembershipRemoved(Identifier id, Identifier membershipId, Identifier organizationId,
        Identifier uninviterId)
    {
        return new MembershipRemoved(id)
        {
            MembershipId = membershipId,
            OrganizationId = organizationId,
            UnInvitedById = uninviterId
        };
    }

    public static MembershipRoleAssigned MembershipRoleAssigned(Identifier id, Identifier organizationId,
        Identifier membershipId, Role role)
    {
        return new MembershipRoleAssigned(id)
        {
            OrganizationId = organizationId,
            MembershipId = membershipId,
            Role = role.Identifier
        };
    }

    public static MembershipRoleUnassigned MembershipRoleUnassigned(Identifier id, Identifier organizationId,
        Identifier membershipId, Role role)
    {
        return new MembershipRoleUnassigned(id)
        {
            OrganizationId = organizationId,
            MembershipId = membershipId,
            Role = role.Identifier
        };
    }

    public static PlatformFeatureAssigned PlatformFeatureAssigned(Identifier id, Feature feature)
    {
        return new PlatformFeatureAssigned(id)
        {
            Feature = feature.Identifier
        };
    }

    public static PlatformFeaturesReset PlatformFeaturesReset(Identifier id, Identifier assignerId, Features features)
    {
        return new PlatformFeaturesReset(id)
        {
            AssignedById = assignerId,
            Features = features.Items.Select(x => x.Identifier).ToList()
        };
    }

    public static PlatformFeatureUnassigned PlatformFeatureUnassigned(Identifier id, Feature feature)
    {
        return new PlatformFeatureUnassigned(id)
        {
            Feature = feature.Identifier
        };
    }

    public static PlatformRoleAssigned PlatformRoleAssigned(Identifier id, Role role)
    {
        return new PlatformRoleAssigned(id)
        {
            Role = role.Identifier
        };
    }

    public static PlatformRoleUnassigned PlatformRoleUnassigned(Identifier id, Role role)
    {
        return new PlatformRoleUnassigned(id)
        {
            Role = role.Identifier
        };
    }

    public static Registered Registered(Identifier id, EndUserProfile userProfile, Optional<EmailAddress> username,
        UserClassification classification, UserAccess access, UserStatus status, Roles roles, Features features)
    {
        return new Registered(id)
        {
            Username = username.ValueOrDefault!,
            Classification = classification,
            Access = access,
            Status = status,
            Roles = roles.Denormalize(),
            Features = features.Denormalize(),
            UserProfile = new RegisteredUserProfile
            {
                FirstName = userProfile.Name.FirstName,
                LastName = userProfile.Name.LastName.ValueOrDefault!,
                Timezone = userProfile.Timezone.ToString(),
                CountryCode = userProfile.Address.CountryCode.ToString()
            }
        };
    }
}