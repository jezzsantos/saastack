namespace Application.Interfaces;

/// <summary>
///     Results of a search
/// </summary>
public class SearchResults<TResource>
{
    public List<TResource> Results { get; set; } = new();

    public SearchResultMetadata Metadata { get; set; } = new();
}