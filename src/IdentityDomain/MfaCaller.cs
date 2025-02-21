using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class MfaCaller : ValueObjectBase<MfaCaller>
{
    public static Result<MfaCaller, Error> Create(Identifier callerId, string? authenticationToken)
    {
        return new MfaCaller(callerId, authenticationToken.HasNoValue(), authenticationToken.HasValue()
            ? authenticationToken.ToOptional()
            : Optional<string>.None);
    }

    private MfaCaller(Identifier callerId, bool isAuthenticated, Optional<string> authenticationToken)
    {
        CallerId = callerId;
        IsAuthenticated = isAuthenticated;
        AuthenticationToken = authenticationToken;
    }

    public Optional<string> AuthenticationToken { get; }

    public Identifier CallerId { get; }

    public bool IsAuthenticated { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<MfaCaller> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new MfaCaller(
                parts[0]!.ToId(),
                parts[1]!.ToBoolOrDefault(false),
                parts[2].FromValueOrNone<string?, string>());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { CallerId, IsAuthenticated, AuthenticationToken };
    }
}