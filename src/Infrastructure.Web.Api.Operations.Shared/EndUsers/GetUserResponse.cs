using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

public class GetUserResponse : IWebResponse
{
    public EndUser? User { get; set; }
}