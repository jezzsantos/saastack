extern alias Analyzers;
using Analyzers::Infrastructure.WebApi.Interfaces;
using Analyzers::Tools.Analyzers.Core;
using JetBrains.Annotations;
using Xunit;

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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 6, 17, "AMethod");
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 7, 17, "AMethod");
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 7, 25, "AMethod");
        }

        [Fact]
        public async Task WhenHasPublicMethodWithTaskOfApiEmptyResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Get)]
    public Task<ApiPostResult<TestResource, TestResponse>> AMethod(TestRequest1 request)
    {
        return Task.FromResult<ApiPostResult<TestResource, TestResponse>>(() => new Result<PostResult<TestResponse>, Error>());
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas010, input, 6, 19, "AMethod");
        }

        [Fact]
        public async Task WhenHasPublicMethodWithNakedApiEmptyResultReturnType_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
        return () => new Result<PostResult<TestResponse>, Error>();
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
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 9, 27, "AMethod");
        }

        [Fact]
        public async Task WhenHasTooManyParameters_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 11, 27, "AMethod");
        }

        [Fact]
        public async Task WhenFirstParameterIsNotRequestType_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas011, input, 9, 27, "AMethod");
        }

        [Fact]
        public async Task WhenSecondParameterIsNotCancellationToken_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas012, input, 10, 27, "AMethod");
        }

        [Fact]
        public async Task WhenOnlyRequest_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas013, input, 10, 27, "AMethod");
        }

        [Fact]
        public async Task WhenMissingAttribute_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas013, input, 11, 27, "AMethod");
        }

        [Fact]
        public async Task WhenAttribute_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas014, input, 21, 27, "AMethod3");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas015
    {
        [Fact]
        public async Task WhenNoDuplicateRequests_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
using Infrastructure.WebApi.Common;
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

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas015, input, (11, 27, "AMethod1"),
                (16, 27, "AMethod2"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRuleSas016
    {
        [Fact]
        public async Task WhenDeleteAndApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
        public async Task WhenGetAndApiEmptyResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
        public async Task WhenPostAndApiPostResult_ThenNoAlert()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
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
        public async Task WhenPostAndOtherReturnResult_ThenAlerts()
        {
            const string input = @"
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using System.Threading.Tasks;
using Common;
using Tools.Analyzers.Core.UnitTests;
namespace ANamespace;
public class AClass : IWebApiService
{
    [WebApiRoute(""/aresource"", WebApiOperation.Post)]
    public ApiEmptyResult AMethod1(TestRequest1 request)
    { 
        return () => new Result<EmptyResponse, Error>();
    }
}";

            await Verify.DiagnosticExists<WebApiClassAnalyzer>(WebApiClassAnalyzer.Sas016, input, 11, 27, "AMethod1",
                WebApiOperation.Post, "ApiPostResult<TResource, TResponse>");
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