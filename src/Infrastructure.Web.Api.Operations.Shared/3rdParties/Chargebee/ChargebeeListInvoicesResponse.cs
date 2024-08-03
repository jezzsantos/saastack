using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeListInvoicesResponse : IWebResponse
{
    public List<ChargebeeInvoiceList>? List { get; set; }

    public string? NextOffset { get; set; }
}

[UsedImplicitly]
public class ChargebeeInvoiceList
{
    public ChargebeeInvoice? Invoice { get; set; }
}

[UsedImplicitly]
public class ChargebeeInvoice
{
}