using System.Linq.Expressions;
using Common.Extensions;

namespace Application.Interfaces;

/// <summary>
///     Defines options for embedding resources in GET REST API calls
/// </summary>
public class GetOptions
{
    public const string EmbedRequestParamDelimiter = ",";
    public const string EmbedRequestParamName = "embed";
    public const int MaxResourceReferences = 10;

    public static readonly GetOptions All = new(ExpandOptions.All, new List<string>());
    public static readonly GetOptions None = new(ExpandOptions.None, new List<string>());
    private readonly List<string> _resourceReferences;

    public GetOptions() : this(ExpandOptions.All)
    {
    }

    public GetOptions(ExpandOptions expand, List<string>? childReferences = null, ExpandOptions? @default = null)
    {
        Initial = @default ?? ExpandOptions.All;
        Expand = expand;
        _resourceReferences = childReferences.Exists()
            ? childReferences.Where(cr => cr.HasValue())
                .ToList()
            : new List<string>();
    }

    public ExpandOptions Expand { get; }

    public ExpandOptions Initial { get; }

    public IEnumerable<string> ResourceReferences => _resourceReferences;

    /// <summary>
    ///     Creates a custom set of options for the specified resource properties
    /// </summary>
    public static GetOptions Custom(List<string> resourceReferences)
    {
        return new GetOptions(ExpandOptions.Custom, resourceReferences);
    }

    /// <summary>
    ///     Creates a custom set of options for the specified properties of the specific <see cref="TResource" />
    /// </summary>
    public static GetOptions Custom<TResource>(params Expression<Func<TResource, object?>>[] propertyReferences)
    {
        return Custom(propertyReferences.ReferencesToNames());
    }
}

/// <summary>
///     Defines the options for expanding child resources in REST responses
/// </summary>
public enum ExpandOptions
{
    None = 0,
    Custom = 1,
    All = 2
}