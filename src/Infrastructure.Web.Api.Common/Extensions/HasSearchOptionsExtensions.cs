using Application.Interfaces;
using Application.Interfaces.Extensions;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Common.Extensions;

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
                result.Sort = new Sorting(ParseSortBy(defaultSort), ParseSortDirection(defaultSort));
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
                result.Filter = new Filtering(ParseFilters(defaultFilter));
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

    /// <summary>
    ///     Applies the specified <see cref="searchOptions" /> and <see cref="getOptions" /> to the specified
    ///     <see cref="requestDto" />
    /// </summary>
    public static TRequest WithOptions<TRequest>(this TRequest requestDto, SearchOptions searchOptions,
        GetOptions getOptions)
        where TRequest : IHasSearchOptions
    {
        requestDto.Offset = searchOptions.Offset;
        requestDto.Limit = searchOptions.Limit;
        requestDto.Sort = searchOptions.Sort.HasValue
            ? searchOptions.Sort.Value.ToSort()
            : null;
        requestDto.Filter = searchOptions.Filter.ToFilter();
        requestDto.Embed = getOptions.ToEmbed();

        return requestDto;
    }

    private static string ParseSortBy(string sortBy)
    {
        return sortBy.StartsWith(SearchOptions.SortSigns[0]
            .ToString()) || sortBy.StartsWith(SearchOptions.SortSigns[1]
            .ToString())
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
        return filter.Split(SearchOptions.FilterDelimiters)
            .ToList();
    }
}