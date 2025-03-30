#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of enums in the request
/// </summary>
[Route("/testingonly/general/enum", OperationMethod.Post, isTestingOnly: true)]
public class
    PostWithEnumTestingOnlyRequest : WebRequest<PostWithEnumTestingOnlyRequest, StringMessageTestingOnlyResponse>
{
    public TestEnum? AnEnum { get; set; }

    public string? AProperty { get; set; }
}

public enum TestEnum
{
    Value1,
    Value2,
    Value3
}
#endif