#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/emails/drain", ServiceOperation.Post, AccessType.HMAC, true)]
public class DrainAllEmailsRequest : UnTenantedEmptyRequest
{
}
#endif