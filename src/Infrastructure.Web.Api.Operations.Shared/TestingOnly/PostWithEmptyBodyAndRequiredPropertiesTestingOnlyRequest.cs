#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of an empty body in a POST request that has required properties
/// </summary>
[Route("/testingonly/general/body/empty/required", OperationMethod.Post, isTestingOnly: true)]
[UsedImplicitly]
public class
    PostWithEmptyBodyAndRequiredPropertiesTestingOnlyRequest : WebRequest<
    PostWithEmptyBodyAndRequiredPropertiesTestingOnlyRequest,
    StringMessageTestingOnlyResponse>
{
#pragma warning disable SAASWEB037
    public required string RequiredField { get; set; }
#pragma warning restore SAASWEB037
}

#endif