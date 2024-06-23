using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

public class SearchSubscriptionHistoryResponse : SearchResponse
{
    public List<Invoice>? Invoices { get; set; }
}