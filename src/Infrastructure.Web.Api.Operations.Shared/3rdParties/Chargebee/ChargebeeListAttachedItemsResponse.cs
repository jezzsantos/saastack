using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeListAttachedItemsResponse : IWebResponse
{
    public List<ChargebeeAttachedItemList>? List { get; set; }
}

[UsedImplicitly]
public class ChargebeeAttachedItemList
{
    public ChargebeeAttachedItem? AttachedItem { get; set; }
}

[UsedImplicitly]
public class ChargebeeAttachedItem
{
}