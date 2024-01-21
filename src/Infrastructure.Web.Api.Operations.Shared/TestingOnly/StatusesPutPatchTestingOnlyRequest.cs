#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/statuses/putpatch", ServiceOperation.PutPatch, isTestingOnly: true)]
public class StatusesPutPatchTestingOnlyRequest : IWebRequest<StatusesTestingOnlyResponse>
{
}
#endif