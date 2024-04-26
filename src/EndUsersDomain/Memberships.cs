using System.Collections;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Shared.Organizations;

namespace EndUsersDomain;

public class Memberships : IReadOnlyList<Membership>
{
    private readonly List<Membership> _memberships = new();

    public Membership DefaultMembership => _memberships.First(ms => ms.IsDefault);

    public bool HasPersonalOrganization => _memberships.Any(ms => ms.Ownership == OrganizationOwnership.Personal);

    public Result<Error> EnsureInvariants()
    {
        foreach (var membership in _memberships)
        {
            var invariants = membership.EnsureInvariants();
            if (invariants.IsFailure)
            {
                return invariants.Error;
            }
        }

        if (_memberships.Any())
        {
            if (!_memberships.Any(ms => ms.IsDefault))
            {
                return Error.RuleViolation(Resources.Memberships_NoDefault);
            }

            if (_memberships.Count(ms => ms.IsDefault) > 1)
            {
                return Error.RuleViolation(Resources.Memberships_MultipleDefaults);
            }

            if (_memberships
                .GroupBy(ms => ms.OrganizationId)
                .Any(grp => grp.Count() > 1))
            {
                return Error.RuleViolation(Resources.Memberships_DuplicateMemberships);
            }
        }

        return Result.Ok;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<Membership> GetEnumerator()
    {
        return _memberships.GetEnumerator();
    }

    public int Count => _memberships.Count;

    public Membership this[int index] => _memberships[index];

    public void Add(Membership membership)
    {
        var matching = FindByOrganization(membership);
        if (matching.HasValue)
        {
            _memberships.Remove(matching);
        }

        _memberships.Add(membership);
    }

    public Optional<Membership> FindByMembershipId(Identifier membershipId)
    {
        return _memberships
            .SingleOrDefault(ms => ms.Id == membershipId);
    }

    public Optional<Membership> FindByOrganizationId(Identifier organizationId)
    {
        return _memberships
            .SingleOrDefault(ms => ms.OrganizationId == organizationId);
    }

    public Membership FindNextDefaultMembership()
    {
        var next = _memberships
            .Except(new[] { DefaultMembership })
            // ReSharper disable once SimplifyLinqExpressionUseMinByAndMaxBy
            .OrderByDescending(ms => ms.CreatedAtUtc)
            .FirstOrDefault();

        if (next.NotExists())
        {
            throw new InvalidOperationException(Resources.Memberships_MissingNextDefaultMembership);
        }

        return next;
    }

    public void Remove(Identifier membershipId)
    {
        var membership = _memberships.Find(ms => ms.Id == membershipId);
        if (membership.Exists())
        {
            _memberships.Remove(membership);
        }
    }

    private Optional<Membership> FindByOrganization(Membership membership)
    {
        return _memberships
            .FirstOrDefault(m => m.OrganizationId == membership.OrganizationId);
    }
}