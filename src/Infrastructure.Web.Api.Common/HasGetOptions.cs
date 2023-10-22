using System.Linq.Expressions;
using Application.Interfaces;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common;

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

    /// <summary>
    ///     Converts the <see cref="resourceProperties" /> to a <see cref="HasGetOptions" />
    /// </summary>
    public static HasGetOptions Custom<TResource>(params Expression<Func<TResource, object?>>[] resourceProperties)
    {
        return GetOptions.Custom(resourceProperties)
            .ToHasGetOptions();
    }
}