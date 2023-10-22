namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Options for GET operations
/// </summary>
public interface IHasGetOptions
{
    /// <summary>
    ///     The child resources to embed in the resource
    /// </summary>
    string? Embed { get; set; }
}