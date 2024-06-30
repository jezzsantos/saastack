using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Options for a SEARCH request
/// </summary>
public class HasSearchOptions : IHasSearchOptions
{
    public string? Distinct { get; set; }

    public string? Embed { get; set; }

    public string? Filter { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public string? Sort { get; set; }
}