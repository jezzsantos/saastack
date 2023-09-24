namespace Application.Interfaces;

/// <summary>
///     Results of a search
/// </summary>
public class SearchResults<TResource>
{
    public SearchResultMetadata Metadata { get; set; } = new();

    public List<TResource> Results { get; set; } = new();
}