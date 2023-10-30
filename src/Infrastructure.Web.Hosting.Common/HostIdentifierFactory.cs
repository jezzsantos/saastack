using Domain.Common.Identity;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that registers domain aggregates
/// </summary>
public sealed class HostIdentifierFactory : NamePrefixedIdentifierFactory
{
    internal HostIdentifierFactory(IDictionary<Type, string> prefixes) : base(prefixes)
    {
    }
}