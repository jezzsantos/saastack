using System.Linq.Expressions;
using Common;
using Common.Extensions;

namespace Application.Interfaces;

public static class GetOptionsExtensions
{
    /// <summary>
    ///     Returns whether the named embedded resource should be expanded, according to the <see cref="GetOptions" />
    /// </summary>
    public static bool ShouldExpandEmbeddedResource<TResource>(this GetOptions options,
        Expression<Func<TResource, object?>> propertyReference)
    {
        if (options.NotExists())
        {
            return true;
        }

        return IsResourceIncluded(options, propertyReference);
    }

    internal static List<string> ReferencesToNames<TResource>(
        this Expression<Func<TResource, object?>>[] propertyReferences)
    {
        return propertyReferences.Select(ToResourceReference)
            .ToList();
    }

    private static string ToResourceReference<TResource>(Expression<Func<TResource, object?>> propertyReference)
    {
        var propertyName = Reflector.GetPropertyName(propertyReference);

        return $"{typeof(TResource).Name}.{propertyName}".ToLower();
    }

    private static bool IsResourceIncluded<TResource>(GetOptions options,
        Expression<Func<TResource, object?>> resourceProperty)
    {
        if (options.Expand == ExpandOptions.All)
        {
            return true;
        }

        if (options.Expand == ExpandOptions.None)
        {
            return false;
        }

        var propertyReferences = options.ResourceReferences.ToList();
        if (propertyReferences.HasNone())
        {
            return false;
        }

        return IsMatchResourceReference(propertyReferences, resourceProperty);
    }

    private static bool IsMatchResourceReference<TResource>(IEnumerable<string> expansionProperties,
        Expression<Func<TResource, object?>> propertyReference)
    {
        var resourceReference = ToResourceReference(propertyReference);
        return expansionProperties.Any(x => x.EqualsIgnoreCase(resourceReference));
    }
}