#if TESTINGONLY
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

public class GetUserResponse : IWebResponse
{
    public EndUserWithMemberships? User { get; set; }
}
#endif