#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

[Route("/audits/drain", ServiceOperation.Post, true)]
public class DrainAllAuditsRequest : UnTenantedEmptyRequest
{
}
#endif