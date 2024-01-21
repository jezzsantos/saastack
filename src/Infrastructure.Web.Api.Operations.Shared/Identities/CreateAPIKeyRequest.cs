#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/apikeys", ServiceOperation.Post, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard)]
public class CreateAPIKeyRequest : UnTenantedRequest<CreateAPIKeyResponse>
{
}

#endif