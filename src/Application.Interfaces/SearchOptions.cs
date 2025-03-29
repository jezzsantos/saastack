using Common;

namespace Application.Interfaces;

/// <summary>
///     Defines options for searching
/// </summary>
public class SearchOptions
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 1000;
    public const int NoLimit = 0;
    public const int NoOffset = -1;
    public const char SortSignAscending = '+';
    public const char SortSignDescending = '-';
    public static readonly char[] FilterDelimiters =
    [
        ',',
        ';'
    ];

    public static readonly char[] SortSigns =
    {
        SortSignAscending,
        SortSignDescending
    };

    public static readonly SearchOptions WithMaxLimit = new() { Limit = MaxLimit };

    /// <summary>
    ///     The fields to include in the search results
    /// </summary>
    public Filtering Filter { get; set; } = new();

    /// <summary>
    ///     The maximum number of search results
    /// </summary>
    public int Limit { get; set; } = DefaultLimit;

    /// <summary>
    ///     The offset of the first search result. Zero based.
    /// </summary>
    public int Offset { get; set; } = NoOffset;

    /// <summary>
    ///     How to sort the search results
    /// </summary>
    public Optional<Sorting> Sort { get; set; }

    public void ClearLimitAndOffset()
    {
        Offset = NoOffset;
        Limit = DefaultLimit;
    }
}