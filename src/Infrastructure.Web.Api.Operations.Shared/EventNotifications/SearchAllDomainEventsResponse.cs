using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications
{
    public class SearchAllDomainEventsResponse : SearchResponse
    {
        public List<DomainEvent>? Events { get; set; }
    }
}
#endif