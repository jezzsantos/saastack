namespace Application.Interfaces.Resources;

/// <summary>
///     Defines a resource that has a unique identifier
/// </summary>
public interface IIdentifiableResource
{
    string Id { get; set; }
}