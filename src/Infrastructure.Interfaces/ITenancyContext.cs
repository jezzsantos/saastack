using Application.Interfaces.Services;

namespace Infrastructure.Interfaces;

/// <summary>
///     Defines the context of a tenancy operating on the platform
/// </summary>
public interface ITenancyContext
{
    string? Current { get; }

    public TenantSettings Settings { get; }

    void Set(string id, TenantSettings settings);
}