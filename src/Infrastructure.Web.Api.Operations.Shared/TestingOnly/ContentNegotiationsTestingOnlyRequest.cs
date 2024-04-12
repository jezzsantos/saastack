#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/negotiations/get", OperationMethod.Get, isTestingOnly: true)]
public class ContentNegotiationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Format { get; set; }
}
#endif