extern alias NonPlatformAnalyzers;
using Xunit;
using ApiLayerAnalyzer = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.ApiLayerAnalyzer;
using ApiLayerCodeFix = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.ApiLayerCodeFix;
using UsedImplicitly = NonPlatformAnalyzers::JetBrains.Annotations.UsedImplicitlyAttribute;
namespace Tools.Analyzers.NonPlatform.UnitTests;

[UsedImplicitly]
public class ApiLayerCodeFixSpec
{
    [UsedImplicitly]
    public class GivenARequest
    {
        [Trait("Category", "Unit.Tooling")]
        public class GivenRuleRule039
        {
            [Fact]
            public async Task WhenFixingMissingDocumentationAndHasAttributes_ThenAddsSummary()
            {
                const string problem = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Post)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
[Authorize(Roles.Platform_Operations, Features.Platform_PaidTrial)]
public class ARequest : IWebRequest
{
    public string AProperty { get; set; }
}";
                const string fix = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
/// <summary>
///  A summary
/// </summary>
/// <response code=""405"">A description of an optional specific error response (if desired)</response>
[Route(""/apath"", OperationMethod.Post)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
[Authorize(Roles.Platform_Operations, Features.Platform_PaidTrial)]
public class ARequest : IWebRequest
{
    public string AProperty { get; set; }
}";

                await Verify.CodeFixed<ApiLayerAnalyzer, ApiLayerCodeFix>(
                    ApiLayerAnalyzer.Rule039,
                    problem, fix, 9, 14, "ARequest");
            }
        }
    }
}