using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

public class DeliverMessageResponse : IWebResponse
{
    public bool IsSent { get; set; }
}