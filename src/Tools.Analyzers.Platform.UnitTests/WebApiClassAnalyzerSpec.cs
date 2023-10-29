extern alias Analyzers;
using Analyzers::Application.Interfaces;
using Analyzers::Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Xunit;
using TypeExtensions = Analyzers::Tools.Analyzers.Platform.TypeExtensions;
using WebApiClassAnalyzer = Analyzers::Tools.Analyzers.Platform.WebApiClassAnalyzer;

namespace Tools.Analyzers.Platform.UnitTests;

extern alias Analyzers;

[UsedImplicitly]
public class WebApiClassAnalyzerSpec
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

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenNotWebApiClass_ThenNoAlert()
        {
            const string input = @"
namespace ANamespace;
public class AClass
{
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
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

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
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

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
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

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas010
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 6, 17, "AMethod",
                TypeExtensions.Stringify(WebApiClassAnalyzer.AllowableReturnTypes));
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 7, 17, "AMethod",
                TypeExtensions.Stringify(WebApiClassAnalyzer.AllowableReturnTypes));
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
    public Task<string> AMethod(){ return Task.FromResult(""""); }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 7, 25, "AMethod",
                TypeExtensions.Stringify(WebApiClassAnalyzer.AllowableReturnTypes));
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiEmptyResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiEmptyResult> AMethod(TestGetRequest request)
    { 
        return Task.FromResult<ApiEmptyResult>(() => new Result<EmptyResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiResult<TestResource, TestResponse>> AMethod(TestGetRequest request)
    {
        return Task.FromResult<ApiResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiPostResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiPostResult<TestResource, TestResponse>> AMethod(TestPostRequest request)
    {
        return Task.FromResult<ApiPostResult<TestResource, TestResponse>>(() => new Result<PostResult<TestResponse>, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiGetResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiGetResult<TestResource, TestResponse>> AMethod(TestGetRequest request)
    {
        return Task.FromResult<ApiGetResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiSearchResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiSearchResult<TestResource, TestSearchResponse>> AMethod(TestSearchRequest request)
    {
        return Task.FromResult<ApiSearchResult<TestResource, TestSearchResponse>>(() => new Result<TestSearchResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiPutPatchResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiPutPatchResult<TestResource, TestResponse>> AMethod(TestPutPatchRequest request)
    {
        return Task.FromResult<ApiPutPatchResult<TestResource, TestResponse>>(() => new Result<TestResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiDeleteResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public Task<ApiDeleteResult> AMethod(TestDeleteRequest request)
    {
        return Task.FromResult<ApiDeleteResult>(() => new Result<EmptyResponse, Error>());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithWrongNakedReturnType_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
namespace ANamespace;
public class AClass : IWebApiService
{
    public string AMethod(){ return """"; }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 6, 19, "AMethod",
                TypeExtensions.Stringify(WebApiClassAnalyzer.AllowableReturnTypes));
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiEmptyResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<TestResource, TestResponse> AMethod(TestGetRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiPostResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPostRequest request)
    {
        return () => new Result<PostResult<TestResponse>, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiGetResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<TestResource, TestResponse> AMethod(TestGetRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiSearchResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<TestResource, TestSearchResponse> AMethod(TestSearchRequest request)
    {
        return () => new Result<TestSearchResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiPutPatchResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<TestResource, TestResponse> AMethod(TestPutPatchRequest request)
    {
        return () => new Result<TestResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiDeleteResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestDeleteRequest request)
    {
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas011AndSas012
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 8, 27, "AMethod");
        }

        [Fact]
        public async Task WhenHasTooManyParameters_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request, CancellationToken cancellationToken, string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 10, 27, "AMethod");
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 8, 27, "AMethod");
        }

        [Fact]
        public async Task WhenSecondParameterIsNotCancellationToken_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request, string value)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas012, input, 9, 27, "AMethod");
        }

        [Fact]
        public async Task WhenOnlyRequest_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenRequestAndCancellation_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request, CancellationToken cancellationToken)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas013
    {
        [Fact]
        public async Task WhenHasNoAttributes_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestNoneRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(input, (WebApiClassAnalyzer.Sas013, 9, 27, "AMethod"),
                (WebApiClassAnalyzer.Sas017, 9, 35, "TestNoneRequest"));
        }

        [Fact]
        public async Task WhenMissingAttribute_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [TestAttribute]
    public ApiEmptyResult AMethod(TestNoneRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(input, (WebApiClassAnalyzer.Sas013, 10, 27, "AMethod"),
                (WebApiClassAnalyzer.Sas017, 10, 35, "TestNoneRequest"));
        }

        [Fact]
        public async Task WhenAttribute_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas014
    {
        [Fact]
        public async Task WhenOneRoute_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenTwoWithSameRoute_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenThreeWithSameRouteFirstSegment_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod3(TestGetRequest3 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenDifferentRouteSegments_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod4(TestGetRequest4 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas014, input, 17, 27, "AMethod4");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas015
    {
        [Fact]
        public async Task WhenNoDuplicateRequests_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenDuplicateRequests_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod1(TestGetRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod2(TestGetRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    public ApiEmptyResult AMethod3(TestGetRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas015, input, (9, 27, "AMethod1"),
                (13, 27, "AMethod2"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas016
    {
        [Fact]
        public async Task WhenPostAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestPostRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenGetAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestSearchRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestDeleteRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPostAndReturnsApiResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestPostRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 44, "AMethod",
                ServiceOperation.Post, ExpectedAllowedResultTypes(ServiceOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestGetRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestSearchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiResult<string, TestResponse> AMethod(TestDeleteRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPostAndReturnsApiPostResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPostRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenGetAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestGetRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 54, "AMethod",
                ServiceOperation.Get, ExpectedAllowedResultTypes(ServiceOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestSearchRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 54, "AMethod",
                ServiceOperation.Search, ExpectedAllowedResultTypes(ServiceOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestPutPatchRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 54, "AMethod",
                ServiceOperation.PutPatch, ExpectedAllowedResultTypes(ServiceOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPostResult<TestResource, TestResponse> AMethod(TestDeleteRequest request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 54, "AMethod",
                ServiceOperation.Delete, ExpectedAllowedResultTypes(ServiceOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestPostRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 47, "AMethod",
                ServiceOperation.Post, ExpectedAllowedResultTypes(ServiceOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiGetResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestGetRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestSearchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 47, "AMethod",
                ServiceOperation.PutPatch, ExpectedAllowedResultTypes(ServiceOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiGetResult<string, TestResponse> AMethod(TestDeleteRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 47, "AMethod",
                ServiceOperation.Delete, ExpectedAllowedResultTypes(ServiceOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestPostRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 56, "AMethod",
                ServiceOperation.Post, ExpectedAllowedResultTypes(ServiceOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestGetRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 56, "AMethod",
                ServiceOperation.Get, ExpectedAllowedResultTypes(ServiceOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiSearchResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestSearchRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 56, "AMethod",
                ServiceOperation.PutPatch, ExpectedAllowedResultTypes(ServiceOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestDeleteRequest request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 56, "AMethod",
                ServiceOperation.Delete, ExpectedAllowedResultTypes(ServiceOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestPostRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 52, "AMethod",
                ServiceOperation.Post, ExpectedAllowedResultTypes(ServiceOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestGetRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 52, "AMethod",
                ServiceOperation.Get, ExpectedAllowedResultTypes(ServiceOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestSearchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 52, "AMethod",
                ServiceOperation.Search, ExpectedAllowedResultTypes(ServiceOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiPutPatchResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiPutPatchResult<string, TestResponse> AMethod(TestDeleteRequest request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 52, "AMethod",
                ServiceOperation.Delete, ExpectedAllowedResultTypes(ServiceOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestPostRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 28, "AMethod",
                ServiceOperation.Post, ExpectedAllowedResultTypes(ServiceOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestGetRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 28, "AMethod",
                ServiceOperation.Get, ExpectedAllowedResultTypes(ServiceOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestSearchRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 28, "AMethod",
                ServiceOperation.Search, ExpectedAllowedResultTypes(ServiceOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestPutPatchRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 9, 28, "AMethod",
                ServiceOperation.PutPatch, ExpectedAllowedResultTypes(ServiceOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiDeleteResult_ThenNotAlert()
        {
            const string input = @"
using Infrastructure.Web.Api.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Platform.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiDeleteResult AMethod(TestDeleteRequest request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        private static string ExpectedAllowedResultTypes(ServiceOperation operation)
        {
            return TypeExtensions.Stringify(WebApiClassAnalyzer.AllowableOperationReturnTypes[operation].ToArray());
        }
    }
}

[UsedImplicitly]
public class TestResource
{
}

[UsedImplicitly]
public class TestResponse : IWebResponse
{
}

[UsedImplicitly]
public class TestSearchResponse : IWebSearchResponse
{
    public SearchResultMetadata? Metadata { get; set; }
}

[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestNoneRequest : IWebRequest<TestResponse>
{
}

[Route("/aresource", ServiceOperation.Search)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestSearchRequest : IWebRequest<TestResponse>
{
}

[Route("/aresource", ServiceOperation.Post)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestPostRequest : IWebRequest<TestResponse>
{
}

[Route("/aresource", ServiceOperation.Get)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestGetRequest : IWebRequest<TestResponse>
{
}

[Route("/aresource/1", ServiceOperation.Get)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestGetRequest1 : IWebRequest<TestResponse>
{
}

[Route("/aresource/2", ServiceOperation.Get)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestGetRequest2 : IWebRequest<TestResponse>
{
}

[Route("/aresource/3", ServiceOperation.Get)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestGetRequest3 : IWebRequest<TestResponse>
{
}

[Route("/anotherresource/1", ServiceOperation.Get)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestGetRequest4 : IWebRequest<TestResponse>
{
}

[Route("/aresource", ServiceOperation.PutPatch)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestPutPatchRequest : IWebRequest<TestResponse>
{
}

[Route("/aresource", ServiceOperation.Delete)]
[Analyzers::JetBrains.Annotations.UsedImplicitlyAttribute]
public class TestDeleteRequest : IWebRequest<TestResponse>
{
}

[AttributeUsage(AttributeTargets.Method)]
[UsedImplicitly]
public class TestAttribute : Attribute
{
}