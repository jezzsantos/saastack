extern alias Analyzers;
using Analyzers::Application.Interfaces;
using Analyzers::Infrastructure.WebApi.Interfaces;
using JetBrains.Annotations;
using Xunit;
using TypeExtensions = Analyzers::Tools.Analyzers.Core.TypeExtensions;
using WebApiClassAnalyzer = Analyzers::Tools.Analyzers.Core.WebApiClassAnalyzer;

namespace Tools.Analyzers.Core.UnitTests;

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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public Task<ApiEmptyResult> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public Task<ApiResult<TestResource, TestResponse>> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public Task<ApiPostResult<TestResource, TestResponse>> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public Task<ApiGetResult<TestResource, TestResponse>> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public Task<ApiSearchResult<TestResource, TestSearchResponse>> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public Task<ApiPutPatchResult<TestResource, TestResponse>> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public Task<ApiDeleteResult> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiResult<TestResource, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiGetResult<TestResource, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiSearchResult<TestResource, TestSearchResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiPutPatchResult<TestResource, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiDeleteResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestRequest1 request, CancellationToken cancellationToken, string value)
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
using Infrastructure.WebApi.Interfaces;
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestRequest1 request, string value)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request, CancellationToken cancellationToken)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    public ApiEmptyResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas013, input, 9, 27, "AMethod");
        }

        [Fact]
        public async Task WhenMissingAttribute_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [TestAttribute]
    public ApiEmptyResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas013, input, 10, 27, "AMethod");
        }

        [Fact]
        public async Task WhenAttribute_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod2(TestRequest2 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource/1"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource/2"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod2(TestRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource/3"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod3(TestRequest3 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource/1"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource/2"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod2(TestRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/anotherresource/1"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod3(TestRequest3 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas014, input, 20, 27, "AMethod3");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas015
    {
        [Fact]
        public async Task WhenNoDuplicateRequests_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod2(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod3(TestRequest2 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas015, input, (10, 27, "AMethod1"),
                (15, 27, "AMethod2"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas016
    {
        [Fact]
        public async Task WhenPostAndReturnsApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiEmptyResult AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 44, "AMethod",
                WebApiOperation.Post, ExpectedAllowedResultTypes(WebApiOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 54, "AMethod",
                WebApiOperation.Get, ExpectedAllowedResultTypes(WebApiOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 54, "AMethod",
                WebApiOperation.Search, ExpectedAllowedResultTypes(WebApiOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 54, "AMethod",
                WebApiOperation.PutPatch, ExpectedAllowedResultTypes(WebApiOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiPostResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiPostResult<TestResource, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new PostResult<TestResponse>(new TestResponse(), ""/alocation"");
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 54, "AMethod",
                WebApiOperation.Delete, ExpectedAllowedResultTypes(WebApiOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiGetResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 47, "AMethod",
                WebApiOperation.Post, ExpectedAllowedResultTypes(WebApiOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiGetResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiGetResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiGetResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiGetResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 47, "AMethod",
                WebApiOperation.PutPatch, ExpectedAllowedResultTypes(WebApiOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiGetResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiGetResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 47, "AMethod",
                WebApiOperation.Delete, ExpectedAllowedResultTypes(WebApiOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 56, "AMethod",
                WebApiOperation.Post, ExpectedAllowedResultTypes(WebApiOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 56, "AMethod",
                WebApiOperation.Get, ExpectedAllowedResultTypes(WebApiOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiSearchResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 56, "AMethod",
                WebApiOperation.PutPatch, ExpectedAllowedResultTypes(WebApiOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiSearchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiSearchResult<string, TestSearchResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestSearchResponse, Error>(new TestSearchResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 56, "AMethod",
                WebApiOperation.Delete, ExpectedAllowedResultTypes(WebApiOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiPutPatchResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 52, "AMethod",
                WebApiOperation.Post, ExpectedAllowedResultTypes(WebApiOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiPutPatchResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 52, "AMethod",
                WebApiOperation.Get, ExpectedAllowedResultTypes(WebApiOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiPutPatchResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiPutPatchResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 52, "AMethod",
                WebApiOperation.Search, ExpectedAllowedResultTypes(WebApiOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiPutPatchResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiPutPatchResult<string, TestResponse> AMethod(TestRequest1 request)
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
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiPutPatchResult<string, TestResponse> AMethod(TestRequest1 request)
    { 
        return () => new Result<TestResponse, Error>(new TestResponse());
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 52, "AMethod",
                WebApiOperation.Delete, ExpectedAllowedResultTypes(WebApiOperation.Delete));
        }

        [Fact]
        public async Task WhenPostAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiDeleteResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 28, "AMethod",
                WebApiOperation.Post, ExpectedAllowedResultTypes(WebApiOperation.Post));
        }

        [Fact]
        public async Task WhenGetAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public ApiDeleteResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 28, "AMethod",
                WebApiOperation.Get, ExpectedAllowedResultTypes(WebApiOperation.Get));
        }

        [Fact]
        public async Task WhenSearchAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Search)]
    public ApiDeleteResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 28, "AMethod",
                WebApiOperation.Search, ExpectedAllowedResultTypes(WebApiOperation.Search));
        }

        [Fact]
        public async Task WhenPutPatchAndReturnsApiDeleteResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.PutPatch)]
    public ApiDeleteResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 10, 28, "AMethod",
                WebApiOperation.PutPatch, ExpectedAllowedResultTypes(WebApiOperation.PutPatch));
        }

        [Fact]
        public async Task WhenDeleteAndReturnsApiDeleteResult_ThenNotAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Delete)]
    public ApiDeleteResult AMethod(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.NoDiagnosticExists<WebApiClassAnalyzer>(input);
        }

        private static string ExpectedAllowedResultTypes(WebApiOperation operation)
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

[UsedImplicitly]
public class TestRequest1 : IWebRequest<TestResponse>
{
}

[UsedImplicitly]
public class TestRequest2 : IWebRequest<TestResponse>
{
}

[UsedImplicitly]
public class TestRequest3 : IWebRequest<TestResponse>
{
}

[AttributeUsage(AttributeTargets.Method)]
[UsedImplicitly]
public class TestAttribute : Attribute
{
}