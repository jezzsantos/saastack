using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeListItemEntitlementsResponse : IWebResponse
{
    public List<ChargebeeItemEntitlementList>? List { get; set; }
}

[UsedImplicitly]
public class ChargebeeItemEntitlementList
{
    public ChargebeeItemEntitlement? ItemEntitlement { get; set; }
}

[UsedImplicitly]
public class ChargebeeItemEntitlement
{
}