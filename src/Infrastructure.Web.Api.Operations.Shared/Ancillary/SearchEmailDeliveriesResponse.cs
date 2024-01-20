using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

public class SearchEmailDeliveriesResponse : SearchResponse
{
    public List<DeliveredEmail>? Emails { get; set; }
}