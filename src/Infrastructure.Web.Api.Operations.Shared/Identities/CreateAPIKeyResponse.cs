#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class CreateAPIKeyResponse : IWebResponse
{
    public string? ApiKey { get; set; }
}
#endif