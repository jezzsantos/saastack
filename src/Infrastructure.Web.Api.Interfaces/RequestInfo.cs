namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the declared information about a <see cref="IWebRequest" />
/// </summary>
public class RequestInfo
{
    public required bool IsTestingOnly { get; init; }

    public required OperationMethod Method { get; init; }

    public required string Route { get; init; }
}