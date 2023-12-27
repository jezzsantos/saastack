#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/usages/drain", ServiceOperation.Post, AccessType.HMAC, true)]
public class DrainAllUsagesRequest : UnTenantedEmptyRequest
{
}
#endif