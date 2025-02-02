namespace Application.Interfaces.Extensions;

public static class SortingExtensions
{
    /// <summary>
    ///     Converts a <see cref="Sorting" /> to its representation on the wire
    /// </summary>
    public static string ToSort(this Sorting sorting)
    {
        return sorting.Direction == SortDirection.Ascending
            ? $"{sorting.By}"
            : $"{SearchOptions.SortSignDescending}{sorting.By}";
    }
}