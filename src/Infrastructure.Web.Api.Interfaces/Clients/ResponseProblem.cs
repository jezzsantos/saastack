namespace Infrastructure.Web.Api.Interfaces.Clients;

/// <summary>
///     Defines a problem returned in a response
/// </summary>
public struct ResponseProblem
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int? Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    public string? Exception { get; set; }

    public ValidatorProblem[]? Errors { get; set; }
}