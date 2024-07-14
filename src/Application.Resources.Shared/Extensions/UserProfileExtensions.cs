using Common.Extensions;

namespace Application.Resources.Shared.Extensions;

public static class UserProfileExtensions
{
    public static string FullName(this PersonName name)
    {
        return name.LastName.HasValue()
            ? $@"{name.FirstName} {name.LastName}"
            : name.FirstName;
    }
}