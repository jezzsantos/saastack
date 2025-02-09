using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.EventNotifications
{
    public class SearchAllEventNotificationsResponse : SearchResponse
    {
        public List<EventNotification> Notifications { get; set; } = [];
    }
}
#endif