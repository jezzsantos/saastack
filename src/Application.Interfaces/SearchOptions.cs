using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using Common;
using Common.Extensions;

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
    {
        ',',
        ';'
    };

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
    ///     The offset of the first search result
    /// </summary>
    public int Offset { get; set; } = NoOffset;

    /// <summary>
    ///     How to sort the search results
    /// </summary>
    public Optional<Sorting> Sort { get; set; }

    public SearchResults<TResult> ApplyWithMetadata<TResult>(IEnumerable<TResult> results)
    {
        return ApplyWithMetadata(results, SearchOptions<TResult>.DynamicOrderByFunc);
    }

    public void ClearLimitAndOffset()
    {
        Offset = NoOffset;
        Limit = DefaultLimit;
    }

    private SearchResults<TResult> ApplyWithMetadata<TResult>(IEnumerable<TResult> results,
        Func<IEnumerable<TResult>, Sorting, IEnumerable<TResult>> orderByFunc)
    {
        var searchResults = new SearchResults<TResult>
        {
            Metadata = this.ToMetadata()
        };

        var unsorted = results.ToList();
        searchResults.Metadata.Total = unsorted.Count;

        if (Sort.HasValue)
        {
            unsorted = orderByFunc(unsorted, Sort.Value)
                .ToList();
        }

        IEnumerable<TResult> unPaged = unsorted.ToArray();

        if (IsOffSet())
        {
            unPaged = unPaged.Skip(Offset);
        }

        if (IsLimited())
        {
            var limit = Math.Min(MaxLimit, Limit);
            unPaged = unPaged.Take(limit);
        }
        else
        {
            unPaged = unPaged.Take(DefaultLimit);
        }

        searchResults.Results = unPaged.ToList();

        return searchResults;
    }

    private bool IsLimited()
    {
        return Limit > NoLimit;
    }

    private bool IsOffSet()
    {
        return Offset > NoOffset;
    }
}

internal static class SearchOptions<TResult>
{
    public static readonly Func<IEnumerable<TResult>, Sorting, IEnumerable<TResult>> DynamicOrderByFunc =
        (items, sorting) =>
        {
            var by = sorting.By;
            if (by.HasNoValue())
            {
                return items;
            }

            var expression = sorting.Direction switch
            {
                SortDirection.Ascending => $"{by} ascending",
                SortDirection.Descending => $"{by} descending",
                _ => throw new InvalidOperationException(nameof(sorting.Direction))
            };

            var itemsToSort = items.ToArray();
            try
            {
                return itemsToSort.AsQueryable()
                    .OrderBy(expression);
            }
            catch (ParseException)
            {
                // Ignore exception. Possibly an invalid sorting expression?
                return itemsToSort;
            }
        };
}