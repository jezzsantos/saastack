using Domain.Interfaces.Validations;

namespace AncillaryDomain;

public static class Validations
{
    public static class EmailDelivery
    {
        public static readonly Validation MessageId = CommonValidations.MessageQueues.Ids.Id;
    }
}