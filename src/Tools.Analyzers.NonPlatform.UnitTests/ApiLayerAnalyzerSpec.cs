extern alias NonPlatformAnalyzers;
extern alias CommonAnalyzers;
using CommonAnalyzers::Tools.Analyzers.Common;
using NonPlatformAnalyzers::Infrastructure.Web.Api.Interfaces;
using Xunit;
using TypeExtensions = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.TypeExtensions;
using IWebResponse = NonPlatformAnalyzers::Infrastructure.Web.Api.Interfaces.IWebResponse;
using IWebSearchResponse = NonPlatformAnalyzers::Infrastructure.Web.Api.Interfaces.IWebSearchResponse;
using SearchResultMetadata = NonPlatformAnalyzers::Application.Interfaces.SearchResultMetadata;
using AccessType = NonPlatformAnalyzers::Infrastructure.Web.Api.Interfaces.AccessType;
using ApiLayerAnalyzer = NonPlatformAnalyzers::Tools.Analyzers.NonPlatform.ApiLayerAnalyzer;
using Roles = NonPlatformAnalyzers::Infrastructure.Web.Api.Interfaces.Roles;
using UsedImplicitly = NonPlatformAnalyzers::JetBrains.Annotations.UsedImplicitlyAttribute;

namespace Tools.Analyzers.NonPlatform.UnitTests;

[UsedImplicitly]
public class ApiLayerAnalyzerSpec
{
    [UsedImplicitly]
    public class GivenAWebApiService
    {
        [Trait("Category", "Unit")]
        public class GivenAnyRule
        {
            [Fact]
            public async Task WhenInExcludedNamespace_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace Common;
public class AClass : IWebApiService
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenNotWebApiClass_ThenNoAlert()
            {
                const string input = @"
namespace ANamespace;
public class AClass
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasNoMethods_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPrivateMethod_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    private void AMethod(){}
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasInternalMethod_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    internal void AMethod(){}
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule010
        {
            [Fact]
            public async Task WhenHasPublicMethodWithVoidReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    public void AMethod(){}
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 6, 17, "AMethod",
                    TypeExtensions.Stringify(
                        ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task AMethod(){ return Task.CompletedTask; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 7, 17, "AMethod",
                    TypeExtensions.Stringify(
                        ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithWrongTaskReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<string> AMethod(){ return Task.FromResult(string.Empty); }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 7, 25, "AMethod",
                    TypeExtensions.Stringify(
                        ApiLayerAnalyzer.AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiEmptyResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiEmptyResult> AMethod(TestGetRouteAttributeRequest request)
    { 
        return Task.FromResult<ApiEmptyResult>(() => new Result<EmptyResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiResult<TestResource, TestResponse>> AMethod(TestGetRouteAttributeRequest request)
    {
        return Task.FromResult<ApiResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiPostResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiPostResult<TestResource, TestResponse>> AMethod(TestPostRouteAttributeRequest request)
    {
        return Task.FromResult<ApiPostResult<TestResource, TestResponse>>(() => new Result<PostResult<TestResponse>, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiGetResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiGetResult<TestResource, TestResponse>> AMethod(TestGetRouteAttributeRequest request)
    {
        return Task.FromResult<ApiGetResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiSearchResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiSearchResult<TestResource, TestSearchResponse>> AMethod(TestSearchRouteAttributeRequest request)
    {
        return Task.FromResult<ApiSearchResult<TestResource, TestSearchResponse>>(() => new Result<TestSearchResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiPutPatchResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiPutPatchResult<TestResource, TestResponse>> AMethod(TestPutPatchRouteAttributeRequest request)
    {
        return Task.FromResult<ApiPutPatchResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithTaskOfApiDeleteResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiDeleteResult> AMethod(TestDeleteRouteAttributeRequest request)
    {
        return Task.FromResult<ApiDeleteResult>(() => new Result<EmptyResponse, Error>());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithWrongNakedReturnType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    public string AMethod(){ return string.Empty; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule010,
                    input, 6,
                    19, "AMethod",
                    TypeExtensions.Stringify(
                        ApiLayerAnalyzer
                            .AllowableServiceOperationReturnTypes));
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiEmptyResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<TestResource, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiPostResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPostRouteAttributeRequest request)
    {
        return () => new Result<PostResult<TestResponse>, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiGetResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<TestResource, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiSearchResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<TestResource, TestSearchResponse> AMethod(TestSearchRouteAttributeRequest request)
    {
        return () => new Result<TestSearchResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiPutPatchResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<TestResource, TestResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenHasPublicMethodWithNakedApiDeleteResultReturnType_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestDeleteRouteAttributeRequest request)
    {
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule011And012
        {
            [Fact]
            public async Task WhenHasNoParameters_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod()
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 8, 27, "AMethod");
            }

            [Fact]
            public async Task WhenHasTooManyParameters_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request, CancellationToken cancellationToken, string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 10, 27, "AMethod");
            }

            [Fact]
            public async Task WhenFirstParameterIsNotRequestType_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule011,
                    input, 8, 27, "AMethod");
            }

            [Fact]
            public async Task WhenSecondParameterIsNotCancellationToken_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request, string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule012,
                    input, 9, 27, "AMethod");
            }

            [Fact]
            public async Task WhenOnlyRequest_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRequestAndCancellation_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request, CancellationToken cancellationToken)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule013AndRule017
        {
            [Fact]
            public async Task WhenHasNoAttributes_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestNoRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule013, 9, 27, "AMethod", null),
                    (ApiLayerAnalyzer.Rule017, 9, 35, "TestNoRouteAttributeRequest", null));
            }

            [Fact]
            public async Task WhenMissingAttribute_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [TestAttribute]
    public ApiEmptyResult AMethod(TestNoRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule013, 10, 27, "AMethod", null),
                    (ApiLayerAnalyzer.Rule017, 10, 35, "TestNoRouteAttributeRequest", null));
            }

            [Fact]
            public async Task WhenAttribute_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule014
        {
            [Fact]
            public async Task WhenOneRoute_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenTwoWithSameRoute_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRouteAttributeRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenThreeWithSameRouteFirstSegment_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRouteAttributeRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod3(TestGetRouteAttributeRequest3 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDifferentRouteSegments_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRouteAttributeRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod4(TestGetRouteAttributeRequest4 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule014,
                    input, 17, 27, "AMethod4");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule015
        {
            [Fact]
            public async Task WhenNoDuplicateRequests_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDuplicateRequests_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod3(TestGetRouteAttributeRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(input,
                    (ApiLayerAnalyzer.Rule015, 9, 27, "AMethod1", null),
                    (ApiLayerAnalyzer.Rule020, 9, 27, "AMethod1", null),
                    (ApiLayerAnalyzer.Rule015, 13, 27, "AMethod2", null),
                    (ApiLayerAnalyzer.Rule020, 13, 27, "AMethod2", null));
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule016
        {
            [Fact]
            public async Task WhenPostAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiEmptyResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPostAndReturnsApiResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 44, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPostAndReturnsApiPostResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenGetAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiPostResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 54, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiGetResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiGetResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 47, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiSearchResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiSearchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 56, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiPutPatchResult_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiPutPatchResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 52, "AMethod", OperationMethod.Delete,
                    ExpectedAllowedResultTypes(OperationMethod.Delete));
            }

            [Fact]
            public async Task WhenPostAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestPostRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Post, ExpectedAllowedResultTypes(OperationMethod.Post));
            }

            [Fact]
            public async Task WhenGetAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Get, ExpectedAllowedResultTypes(OperationMethod.Get));
            }

            [Fact]
            public async Task WhenSearchAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestSearchRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.Search,
                    ExpectedAllowedResultTypes(OperationMethod.Search));
            }

            [Fact]
            public async Task WhenPutPatchAndReturnsApiDeleteResult_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestPutPatchRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule016,
                    input, 9, 28, "AMethod", OperationMethod.PutPatch,
                    ExpectedAllowedResultTypes(OperationMethod.PutPatch));
            }

            [Fact]
            public async Task WhenDeleteAndReturnsApiDeleteResult_ThenNotAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestDeleteRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            private static string ExpectedAllowedResultTypes(OperationMethod method)
            {
                return TypeExtensions.Stringify(
                    ApiLayerAnalyzer
                        .AllowableOperationReturnTypes[method].ToArray());
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule018AndRule019
        {
            [Fact]
            public async Task WhenRouteIsAnonymousAndMissingAuthorizeAttribute_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestAnonymousRouteNoAuthorizeAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRouteIsNotAnonymousAndAuthorizeAttribute_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestSecureRouteAuthorizeAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenRouteIsAnonymousAndAuthorizeAttribute_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestAnonymousRouteAuthorizeAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";
                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule018,
                    input, 9, 35, "TestAnonymousRouteAuthorizeAttributeRequest");
            }

            [Fact]
            public async Task WhenRouteIsNotAnonymousAndNoAuthorizeAttribute_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestSecureRouteNoAuthorizeAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";
                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule019,
                    input, 9, 35, "TestSecureRouteNoAuthorizeAttributeRequest");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule020
        {
            [Fact]
            public async Task WhenNoDuplicateRequests_ThenNoAlert()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRouteAttributeRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }

            [Fact]
            public async Task WhenDuplicateRequests_ThenAlerts()
            {
                const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.NonPlatform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRouteAttributeRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRouteAttributeRequest5 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod3(TestGetRouteAttributeRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(ApiLayerAnalyzer.Rule020,
                    input,
                    (9, 27, "AMethod1"),
                    (13, 27, "AMethod2"));
            }
        }
    }

    [UsedImplicitly]
    public class GivenARequest
    {
        [Trait("Category", "Unit")]
        public class GivenRule030
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
internal class ARequest : IWebRequest
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule030, input, 6, 16, "ARequest");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule031
        {
            [Fact]
            public async Task WhenIsNotNamedCorrectly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class AClass : IWebRequest
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule031, input, 6, 14, "AClass");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule032
        {
            [Fact]
            public async Task WhenIsNotInCorrectAssembly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace anamespace;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule032, input, 6, 14, "ARequest",
                    AnalyzerConstants.ServiceOperationTypesNamespace);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule033
        {
            [Fact]
            public async Task WhenHasNoRouteAttribute_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class ARequest : IWebRequest
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule033, input, 5, 14, "ARequest");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule034
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public ARequest(string value)
    {
        AProperty = value;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule034, input, 6, 14, "ARequest");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    private ARequest()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule034, input, 6, 14, "ARequest");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public ARequest()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule035
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule035, input, 8, 20, "AProperty");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule036
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule036, input, 9, 29, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
[Route(""/apath"", OperationMethod.Get)]
public class ARequest : IWebRequest
{
    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }
    }

    [UsedImplicitly]
    public class GivenAResponse
    {
        [Trait("Category", "Unit")]
        public class GivenRule040
        {
            [Fact]
            public async Task WhenIsNotPublic_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
internal class AResponse : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule040, input, 5, 16, "AResponse");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule041
        {
            [Fact]
            public async Task WhenIsNotNamedCorrectly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AClass : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule041, input, 5, 14, "AClass");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule042
        {
            [Fact]
            public async Task WhenIsNotInCorrectAssembly_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace anamespace;
public class AResponse : IWebResponse
{
    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule042, input, 5, 14, "AResponse",
                    AnalyzerConstants.ServiceOperationTypesNamespace);
            }
        }

        [Trait("Category", "Unit")]
        [Trait("Category", "Unit")]
        public class GivenRule043
        {
            [Fact]
            public async Task WhenHasCtorAndNotParameterless_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public AResponse(string value)
    {
        AProperty = value;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule043, input, 5, 14, "AResponse");
            }

            [Fact]
            public async Task WhenHasCtorAndPrivate_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    private AResponse()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule043, input, 5, 14, "AResponse");
            }

            [Fact]
            public async Task WhenHasCtorAndIsParameterless_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public AResponse()
    {
        AProperty = string.Empty;
    }

    public required string AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule044
        {
            [Fact]
            public async Task WhenAnyPropertyHasNoSetter_ThenAlerts()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public string? AProperty { get; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule044, input, 7, 20, "AProperty");
            }
        }

        [Trait("Category", "Unit")]
        public class GivenRule045
        {
            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsOptional_ThenAlerts()
            {
                const string input = @"
using System;
using Common;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public Optional<string> AProperty { get; set; }
}";

                await Verify.DiagnosticExists<ApiLayerAnalyzer>(
                    ApiLayerAnalyzer.Rule045, input, 8, 29, "AProperty");
            }

            [Fact]
            public async Task WhenAnyPropertyReferenceTypeIsNullable_ThenNoAlert()
            {
                const string input = @"
using System;
using Infrastructure.Web.Api.Interfaces;
namespace Infrastructure.Web.Api.Operations.Shared.Test;
public class AResponse : IWebResponse
{
    public string? AProperty { get; set; }
}";

                await Verify.NoDiagnosticExists<ApiLayerAnalyzer>(input);
            }
        }
    }
}

[UsedImplicitly]
public class TestResource;

[UsedImplicitly]
public class TestResponse : IWebResponse;

[UsedImplicitly]
public class TestSearchResponse : IWebSearchResponse
{
    public SearchResultMetadata? Metadata { get; set; }
}

[UsedImplicitly]
public class TestNoRouteAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Search)]
[UsedImplicitly]
public class TestSearchRouteAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Post)]
[UsedImplicitly]
public class TestPostRouteAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest1 : IWebRequest<TestResponse>;

[Route("/aresource/2", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest2 : IWebRequest<TestResponse>;

[Route("/aresource/3", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest3 : IWebRequest<TestResponse>;

[Route("/anotherresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest4 : IWebRequest<TestResponse>;

[Route("/aresource/1", OperationMethod.Get)]
[UsedImplicitly]
public class TestGetRouteAttributeRequest5 : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.PutPatch)]
[UsedImplicitly]
public class TestPutPatchRouteAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Delete)]
[UsedImplicitly]
public class TestDeleteRouteAttributeRequest : IWebRequest<TestResponse>;

[AttributeUsage(AttributeTargets.Method)]
[UsedImplicitly]
public class TestAttribute : Attribute;

[Route("/aresource", OperationMethod.Post)]
[UsedImplicitly]
public class TestAnonymousRouteNoAuthorizeAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Post)]
[Authorize(Roles.Platform_Standard)]
[UsedImplicitly]
public class TestAnonymousRouteAuthorizeAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard)]
[UsedImplicitly]
public class TestSecureRouteAuthorizeAttributeRequest : IWebRequest<TestResponse>;

[Route("/aresource", OperationMethod.Post, AccessType.Token)]
[UsedImplicitly]
public class TestSecureRouteNoAuthorizeAttributeRequest : IWebRequest<TestResponse>;