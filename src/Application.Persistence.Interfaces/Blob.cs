namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a binary blob of data
/// </summary>
public class Blob
{
    public required string ContentType { get; set; }
}