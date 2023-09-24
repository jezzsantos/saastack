using Common.Extensions;

namespace Domain.Interfaces.Validations;

/// <summary>
///     Provides an <see cref="Validation{TValue}.Expression" /> or <see cref="Validation{TValue}.Function" /> that can
///     validated
/// </summary>
public class Validation : Validation<string>
{
    public Validation(string expression, int? minLength = null, int? maxLength = null,
        IEnumerable<string>? substitutions = null) : base(expression, minLength, maxLength, substitutions)
    {
    }

    public Validation(Func<string, bool> predicate) : base(predicate)
    {
    }
}

/// <summary>
///     Provides an <see cref="Expression" /> or <see cref="Function" /> that can validated
/// </summary>
public class Validation<TValue>
{
    public Validation(string expression, int? minLength = null, int? maxLength = null,
        IEnumerable<string>? substitutions = null)
    {
        Function = null;
        Expression = expression;
        MinLength = minLength;
        MaxLength = maxLength;
        Substitutions = substitutions ?? new List<string>();
    }

    public Validation(Func<TValue, bool> predicate)
    {
        Function = predicate;
        Expression = null;
        MinLength = null;
        MaxLength = null;
        Substitutions = new List<string>();
    }

    public string? Expression { get; }

    public Func<TValue, bool>? Function { get; }

    public int? MaxLength { get; }

    public int? MinLength { get; }

    private IEnumerable<string>? Substitutions { get; }

    /// <summary>
    ///     Substitutes the given values into the expression.
    /// </summary>
    /// <remarks>
    ///     Substitutions are performed by index
    /// </remarks>
    public string Substitute(IEnumerable<string> values)
    {
        return Substitute(InitializeSubstitutions(values));
    }

    /// <summary>
    ///     Substitutes the given name/values into the expression.
    /// </summary>
    private string Substitute(IDictionary<string, string> values)
    {
        if (Expression.HasNoValue())
        {
            return string.Empty;
        }

        var expression = Expression!;
        values.ToList()
            .ForEach(val =>
            {
                if (Substitutions is not null && Substitutions.Contains(val.Key))
                {
                    expression = expression.Replace(val.Key, val.Value);
                }
            });

        return expression;
    }

    private IDictionary<string, string> InitializeSubstitutions(IEnumerable<string> values)
    {
        var result = new Dictionary<string, string>();

        var substitutions = Substitutions?.ToList() ?? new List<string>();
        var counter = 0;
        values.ToList().ForEach(val =>
        {
            if (substitutions.Count > counter)
            {
                result.Add(substitutions[counter], val);

                counter++;
            }
        });

        return result;
    }
}