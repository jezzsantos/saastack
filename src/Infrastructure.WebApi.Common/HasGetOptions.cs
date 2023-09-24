using System.Linq.Expressions;
using Application.Interfaces;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Options for GET requests
/// </summary>
public class HasGetOptions : IHasGetOptions
{
    public const string EmbedAll = "*";
    public const string EmbedNone = "off";

    public static readonly HasGetOptions All = new() { Embed = EmbedAll };
    public static readonly HasGetOptions None = new() { Embed = EmbedNone };

    public string? Embed { get; set; }

    public static HasGetOptions Custom<TResource>(params Expression<Func<TResource, object?>>[] resourceProperties)
    {
        return GetOptions.Custom(resourceProperties).ToHasGetOptions();
    }
}