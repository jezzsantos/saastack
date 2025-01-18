using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

public class SearchAllSmsDeliveriesResponse : SearchResponse
{
    public List<DeliveredSms> Smses { get; set; } = [];
}