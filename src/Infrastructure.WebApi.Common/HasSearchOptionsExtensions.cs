using Application.Interfaces;
using Common.Extensions;
using Infrastructure.WebApi.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Common;

[UsedImplicitly]
public static class HasSearchOptionsExtensions
{
    /// <summary>
    ///     Converts a <see cref="IHasSearchOptions" /> to a <see cref="SearchOptions" />
    /// </summary>
    public static SearchOptions ToSearchOptions(this IHasSearchOptions requestDto, int? defaultLimit = null,
        int? defaultOffset = null, string? defaultSort = null, string? defaultFilter = null)
    {
        if (requestDto.NotExists())
        {
            return new SearchOptions();
        }

        var result = new SearchOptions
        {
            Limit = ToLimit(requestDto, defaultLimit),
            Offset = requestDto.Offset ?? (defaultOffset ?? SearchOptions.NoOffset)
        };

        if (requestDto.Sort.HasValue())
        {
            result.Sort = new Sorting(ParseSortBy(requestDto.Sort!), ParseSortDirection(requestDto.Sort!));
        }
        else
        {
            if (defaultSort.HasValue())
            {
                result.Sort = new Sorting(ParseSortBy(defaultSort!), ParseSortDirection(defaultSort!));
            }
        }

        if (requestDto.Filter.HasValue())
        {
            result.Filter = new Filtering(ParseFilters(requestDto.Filter!));
        }
        else
        {
            if (defaultFilter.HasValue())
            {
                result.Filter = new Filtering(ParseFilters(defaultFilter!));
            }
        }

        return result;

        static int ToLimit(IHasSearchOptions options, int? defaultLimit)
        {
            if (options.Limit.HasValue)
            {
                return options.Limit.Value > SearchOptions.NoLimit
                    ? options.Limit.Value
                    : SearchOptions.DefaultLimit;
            }

            if (defaultLimit.HasValue)
            {
                return defaultLimit.Value > SearchOptions.NoLimit
                    ? defaultLimit.Value
                    : SearchOptions.DefaultLimit;
            }

            return SearchOptions.DefaultLimit;
        }
    }

    private static string ParseSortBy(string sortBy)
    {
        return sortBy.StartsWith(SearchOptions.SortSigns[0].ToString()) ||
               sortBy.StartsWith(SearchOptions.SortSigns[1].ToString())
            ? sortBy.TrimStart(SearchOptions.SortSigns)
            : sortBy;
    }

    private static SortDirection ParseSortDirection(string sort)
    {
        return sort.StartsWith(SearchOptions.SortSignDescending.ToString())
            ? SortDirection.Descending
            : SortDirection.Ascending;
    }

    private static List<string> ParseFilters(string filter)
    {
        return filter.Split(SearchOptions.FilterDelimiters).ToList();
    }
}