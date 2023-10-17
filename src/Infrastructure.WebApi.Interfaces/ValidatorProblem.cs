namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a problem for a specific validator
/// </summary>
public class ValidatorProblem
{
    public required string Reason { get; set; }

    public required string Rule { get; set; }

    public object? Value { get; set; }
}