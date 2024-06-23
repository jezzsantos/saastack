using Common.Extensions;
using Domain.Interfaces.Validations;

namespace Domain.Shared;

public static class Validations
{
    public static class Subscriptions
    {
        public static class Provider
        {
            public static readonly Validation Name = CommonValidations.DescriptiveName(1, 50);
            public static readonly Validation<Dictionary<string, string>> State = new(state =>
            {
                return state.Count > 0 && state.All(pair => pair.Value.Exists());
            });
        }
    }
}