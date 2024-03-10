using Common.Extensions;

namespace Application.Interfaces;

/// <summary>
///     Defines options for filtering the fields of results
/// </summary>
public class Filtering
{
    private readonly List<string> _fields = new();

    public Filtering()
    {
    }

    public Filtering(string field)
    {
        field.ThrowIfNotValuedParameter(nameof(field));
        if (!_fields.Contains(field))
        {
            _fields.Add(field);
        }
    }

    public Filtering(IEnumerable<string> fields)
    {
        foreach (var field in fields)
        {
            if (!_fields.Contains(field))
            {
                _fields.Add(field);
            }
        }
    }

    public IReadOnlyList<string> Fields => _fields;
}