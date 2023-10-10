namespace Application.Interfaces;

/// <summary>
///     Defines options for sorting results
/// </summary>
public class Sorting
{
    public Sorting(string by, SortDirection direction = SortDirection.Ascending)
    {
        ArgumentException.ThrowIfNullOrEmpty(by);
        By = by;
        Direction = direction;
    }

    public string By { get; }

    public SortDirection Direction { get; }

    /// <summary>
    ///     Creates a new <see cref="Sorting" /> for the <see cref="field" /> and <see cref="direction" />
    /// </summary>
    public static Sorting ByField(string field, SortDirection direction = SortDirection.Ascending)
    {
        return new Sorting(field, direction);
    }
}

/// <summary>
///     Defines the sorting direction
/// </summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}