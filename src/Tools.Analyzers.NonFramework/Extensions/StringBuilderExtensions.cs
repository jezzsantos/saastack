using System.Text;

namespace Tools.Analyzers.NonFramework.Extensions;

public static class StringBuilderExtensions
{
    public static void AppendJoin(this StringBuilder builder, string separator, IEnumerable<string> values)
    {
        builder.Append(string.Join(separator, values));
    }
}