using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

public class SearchSmsDeliveriesResponse : SearchResponse
{
    public List<DeliveredSms> Smses { get; set; } = [];
}