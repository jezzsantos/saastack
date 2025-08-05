#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of an array in a GET request
/// </summary>
[Route("/testingonly/general/get/array", OperationMethod.Get, isTestingOnly: true)]
public class
    GetWithSimpleArrayTestingOnlyRequest : WebRequest<GetWithSimpleArrayTestingOnlyRequest,
    StringMessageTestingOnlyResponse>
{
    public string[]? AnArray { get; set; }
}

#endif