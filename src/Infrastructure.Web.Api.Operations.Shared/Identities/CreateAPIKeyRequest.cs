using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/apikeys", ServiceOperation.Post, AccessType.Token, true)]
public class CreateAPIKeyRequest : UnTenantedRequest<CreateAPIKeyResponse>
{
}

#endif