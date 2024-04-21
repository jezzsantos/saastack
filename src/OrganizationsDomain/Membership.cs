using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace OrganizationsDomain;

public sealed class Membership : ValueObjectBase<Membership>
{
    public static Result<Membership, Error> Create(string organizationId, string userId)
    {
        if (organizationId.IsNotValuedParameter(nameof(organizationId), out var error1))
        {
            return error1;
        }

        if (userId.IsNotValuedParameter(nameof(userId), out var error2))
        {
            return error2;
        }

        return new Membership(organizationId, userId.ToId());
    }

    private Membership(string organizationId, Identifier userId)
    {
        UserId = userId;
        OrganizationId = organizationId;
    }

    public string OrganizationId { get; }

    public Identifier UserId { get; }

    public static ValueObjectFactory<Membership> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new Membership(parts[0]!, Identifier.Rehydrate()(parts[1]!, container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { OrganizationId, UserId };
    }
}