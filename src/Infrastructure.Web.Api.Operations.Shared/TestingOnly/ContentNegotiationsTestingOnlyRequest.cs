#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests content negotiation with a specified format
/// </summary>
[Route("/testingonly/negotiations/get", OperationMethod.Get, isTestingOnly: true)]
public class ContentNegotiationsTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Format { get; set; }
}
#endif