using Common.Extensions;
using Domain.Interfaces.Validations;

namespace ImagesDomain;

public static class Validations
{
    public static class Images
    {
        public const long MaxSizeInBytes = 134_217_728; //approx 100MB
        public static readonly IReadOnlyList<string> AllowableContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif"
        };
        public static readonly Validation ContentTypes = new(val =>
        {
            if (val.HasNoValue())
            {
                return false;
            }

            return AllowableContentTypes.ContainsIgnoreCase(val);
        });
        public static readonly Validation Description = CommonValidations.FreeformText(1, 250);
        public static readonly Validation Filename = new(@"^[\d\w\.]*$", 1, 100);
    }
}