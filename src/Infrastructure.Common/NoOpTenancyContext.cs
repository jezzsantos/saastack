using Application.Interfaces.Services;
using Infrastructure.Interfaces;

namespace Infrastructure.Common;

/// <summary>
///     A <see cref="ITenancyContext" /> that does nothing
/// </summary>
public class NoOpTenancyContext : ITenancyContext
{
    public string? Current => null;

    public void Set(string id, TenantSettings settings)
    {
    }

    public TenantSettings Settings => new();
}