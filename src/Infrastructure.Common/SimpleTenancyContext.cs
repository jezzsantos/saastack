using Application.Interfaces.Services;
using Infrastructure.Interfaces;

namespace Infrastructure.Common;

/// <summary>
///     Defines a simple tenancy context that can be set
/// </summary>
public class SimpleTenancyContext : ITenancyContext
{
    public string? Current { get; private set; }

    public void Set(string id, TenantSettings settings)
    {
        Current = id;
        Settings = settings;
    }

    public TenantSettings Settings { get; private set; } = new();
}