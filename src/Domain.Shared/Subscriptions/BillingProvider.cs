using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class BillingProvider : ValueObjectBase<BillingProvider>
{
    public static Result<BillingProvider, Error> Create(string name, SubscriptionMetadata metadata)
    {
        if (name.IsInvalidParameter(Validations.Subscriptions.Provider.Name, nameof(name),
                Resources.BillingProvider_InvalidName,
                out var error1))
        {
            return error1;
        }

        if (metadata.IsInvalidParameter(Validations.Subscriptions.Provider.State, nameof(metadata),
                Resources.BillingProvider_InvalidMetadata, out var error2))
        {
            return error2;
        }

        return new BillingProvider(name, metadata);
    }

    private BillingProvider(string name, SubscriptionMetadata metadata)
    {
        Name = name;
        State = metadata;
    }

    public bool IsInitialized => Name.HasValue();

    public string Name { get; }

    public SubscriptionMetadata State { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<BillingProvider> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new BillingProvider(parts[0]!, parts[1]!.FromJson<SubscriptionMetadata>()!);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Name, State.ToJson(casing: StringExtensions.JsonCasing.Pascal)!];
    }

    public BillingProvider ChangeState(SubscriptionMetadata state)
    {
        return new BillingProvider(Name, state);
    }

    [SkipImmutabilityCheck]
    public bool IsCurrentProvider(string providerName)
    {
        return Name.EqualsIgnoreCase(providerName);
    }
}