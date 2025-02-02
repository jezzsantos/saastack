using Common.Extensions;

namespace Application.Interfaces.Extensions;

public static class FilteringExtensions
{
    /// <summary>
    ///     Converts the <see cref="Filtering" /> to its string representation on the wire
    /// </summary>
    public static string? ToFilter(this Filtering filtering)
    {
        if (filtering.Fields.HasNone())
        {
            return null;
        }

        return filtering.Fields.Join(SearchOptions.FilterDelimiters.First().ToString());
    }
}