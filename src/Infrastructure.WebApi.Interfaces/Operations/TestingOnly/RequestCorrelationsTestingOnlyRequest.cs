#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[Route("testingonly/correlations/get", ServiceOperation.Get, true)]
[UsedImplicitly]
public class RequestCorrelationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif