namespace Common;

/// <summary>
///     Defines the context of the call
/// </summary>
public interface ICallContext
{
    public string CallerId { get; }

    public string CallId { get; }

    public string? TenantId { get; }
}