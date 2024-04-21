using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;

namespace OrganizationsDomain;

public sealed class Memberships : SingleValueObjectBase<Memberships, List<Membership>>
{
    public static readonly Memberships Empty = new(new List<Membership>());

    public static Result<Memberships, Error> Create(List<Membership> value)
    {
        return new Memberships(value);
    }

    private Memberships(List<Membership> value) : base(value)
    {
    }

    private Memberships(IEnumerable<Membership> membership) : base(membership.ToList())
    {
    }

    public int Count => Value.Count;

    public List<Membership> Members => Value;

    public static ValueObjectFactory<Memberships> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var memberships = items.Select(item => Membership.Rehydrate()(item!, container));
            return new Memberships(memberships);
        };
    }

    public Memberships Add(Membership membership)
    {
        if (HasMember(membership.UserId))
        {
            return this;
        }

        var memberships = Value.ToList();
        memberships.Add(membership);
        return new Memberships(memberships);
    }

    [SkipImmutabilityCheck]
    public bool HasMember(Identifier userId)
    {
        return Value.Any(ms => ms.UserId == userId);
    }

    public Memberships Remove(string userId)
    {
        var membership = Value.FirstOrDefault(ms => ms.UserId == userId);
        if (membership is not null)
        {
            var memberships = Value.ToList();
            memberships.Remove(membership);
            return new Memberships(memberships);
        }

        return this;
    }
}