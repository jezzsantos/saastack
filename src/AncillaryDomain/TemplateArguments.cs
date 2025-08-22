using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace AncillaryDomain;

public sealed class TemplateArguments : SingleValueObjectBase<TemplateArguments, List<string>>
{
    public static Result<TemplateArguments, Error> Create()
    {
        return Create(new List<string>());
    }

    public static Result<TemplateArguments, Error> Create(List<string> value)
    {
        return new TemplateArguments(value);
    }

    private TemplateArguments(List<string> arguments) : base(arguments)
    {
    }

    public List<string> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<TemplateArguments> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new TemplateArguments(
                items
                    .Where(item => item.HasValue)
                    .Select(item => item.Value)
                    .ToList());
        };
    }
}