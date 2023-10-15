#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[Route("testingonly/negotiations/get", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ContentNegotiationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Format { get; set; }
}
#endif