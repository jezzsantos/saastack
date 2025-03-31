namespace Infrastructure.Eventing.Common.Extensions;

public static class EventingExtensions
{
    /// <summary>
    ///     Creates the subscription name for the given type and assembly.
    /// </summary>
    public static string CreateSubscriptionName(string fullTypeName, string assemblyName)
    {
        var fullName = fullTypeName.Replace(".", "_");
        return $"{assemblyName}_{fullName}";
    }
}