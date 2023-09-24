using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.Entities;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that creates identifiers that are prefixed with a prefix using the
///     name of the entity.
///     For example: a UserAccount entity could have the prefix "user_"
/// </summary>
public abstract class NamePrefixedIdentifierFactory : IIdentifierFactory
{
    private const string Delimiter = "_";
    private const string UnknownEntityPrefix = "xxx";
    private readonly IDictionary<Type, string> _prefixes;
    private readonly List<string> _supportedPrefixes = new();

    protected NamePrefixedIdentifierFactory(IDictionary<Type, string> prefixes)
    {
        prefixes.Merge(new Dictionary<Type, string>(prefixes) { { typeof(VersionedChangeEvent), "event" } });
        _prefixes = prefixes;
    }

    public Identifier Create(IIdentifiableEntity entity)
    {
        var entityType = entity.GetType();
        var prefix = _prefixes.ContainsKey(entityType)
            ? _prefixes[entity.GetType()]
            : UnknownEntityPrefix;

        var guid = Guid.NewGuid();
        var identifier = ConvertGuid(guid, prefix);

#if TESTINGONLY
        LastCreatedIds.Add(identifier, guid.ToString("D"));
#endif

        return identifier.ToIdentifier();
    }

    public bool IsValid(Identifier value)
    {
        var id = value.ToString();
        var delimiterIndex = id.IndexOf(Delimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return false;
        }

        var prefix = id.Substring(0, delimiterIndex);
        if (!IsKnownPrefix(prefix)
            && prefix != UnknownEntityPrefix)
        {
            return false;
        }

        return CommonValidations.Identifier.Matches(id);
    }

#if TESTINGONLY
    // ReSharper disable once CollectionNeverQueried.Global
    public Dictionary<string, string> LastCreatedIds { get; } = new();
#endif

    public IEnumerable<Type> RegisteredTypes => _prefixes.Keys;

    public IReadOnlyList<string> SupportedPrefixes => _supportedPrefixes;

    public void AddSupportedPrefix(string prefix)
    {
        prefix.GuardAgainstInvalid(CommonValidations.IdentifierPrefix, nameof(prefix));

        _supportedPrefixes.Add(prefix);
    }

    internal static string ConvertGuid(Guid guid, string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        var random = Convert.ToBase64String(guid.ToByteArray())
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty);

        return $"{prefix}{Delimiter}{random}";
    }

    private bool IsKnownPrefix(string prefix)
    {
        var allPossiblePrefixes = _prefixes
            .Select(pre => pre.Value)
            .Concat(SupportedPrefixes)
            .Distinct();

        return allPossiblePrefixes.Contains(prefix);
    }
}