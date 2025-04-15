namespace Infrastructure.Eventing.Common.Extensions;

public static class EventingExtensions
{
    /// <summary>
    ///     Creates the name for a consumer from the given type and assembly.
    ///     Note: Consumer names must be 50 characters or fewer, with only alphanumerics and dashes.
    ///     We deliberately remove common words in their names to reduce the length.
    /// </summary>
    public static string CreateConsumerName(string fullTypeName, string assemblyName)
    {
        var fullName = fullTypeName
            .Replace(".", "-");
        var assem = assemblyName
            .Replace(".", "-");
        var name = $"{assem}-{fullName}"
            .Replace("Infrastructure", string.Empty)
            .Replace("Consumer", string.Empty)
            .Replace("Notifications", string.Empty)
            .Replace("Notification", string.Empty)
            .Replace("--", "-")
            .TrimEnd('-');

        return name
            .Substring(0, Math.Min(name.Length, 50));
    }
}