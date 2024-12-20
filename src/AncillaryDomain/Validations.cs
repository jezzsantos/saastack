using Domain.Interfaces.Validations;

namespace AncillaryDomain;

public static class Validations
{
    public static class EmailDelivery
    {
        public static readonly Validation MessageId = CommonValidations.Messaging.Ids.MessageId;
        public static readonly Validation Tag = new(@"^[\w\d_\.\-]{3,100}$", 3, 100);
    }
}