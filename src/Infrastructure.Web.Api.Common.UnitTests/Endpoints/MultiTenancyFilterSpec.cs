using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public class MultiTenancyFilterSpec
{
    private readonly MultiTenancyFilter _filter;
    private readonly Mock<EndpointFilterDelegate> _next;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<ITenancyContext> _tenancyContext;

    public MultiTenancyFilterSpec()
    {
        _tenancyContext = new Mock<ITenancyContext>();
        _serviceProvider = new Mock<IServiceProvider>();
        _serviceProvider.Setup(sp => sp.GetService(typeof(ITenancyContext)))
            .Returns(_tenancyContext.Object);
        _next = new Mock<EndpointFilterDelegate>();
        _filter = new MultiTenancyFilter();
    }

    [Fact]
    public async Task WhenInvokeAndWrongRequestDelegateSignature_ThenNoRewriteAndContinuesPipeline()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };
        var context = new DefaultEndpointFilterInvocationContext(httpContext);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAndUnTenantedRequestInDelegateSignature_ThenNoRewriteAndContinuesPipeline()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };
        var args = new object[] { new { }, new TestUnTenantedRequest() };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAndNoTenantInTenancyContext_ThenNoRewriteAndContinuesPipeline()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };
        var args = new object[] { new { }, new TestTenantedRequest() };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        context.Arguments[1].As<TestTenantedRequest>().OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task WhenInvokeAndEmptyTenantInTenancyContext_ThenNoRewriteAndContinuesPipeline()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns(string.Empty);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };
        var args = new object[] { new { }, new TestTenantedRequest() };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        context.Arguments[1].As<TestTenantedRequest>().OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task
        WhenInvokeForTenantedRequestAndTenantIdInRequestAlreadyPopulated_ThenNoRewriteAndContinuesPipeline()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns("atenantid");
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };
        var args = new object[]
        {
            new { }, new TestTenantedRequest
            {
                OrganizationId = "anoldorganizationid"
            }
        };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        context.Arguments[1].As<TestTenantedRequest>().OrganizationId.Should().Be("anoldorganizationid");
    }

    [Fact]
    public async Task WhenInvokeForTenantedRequestAndTenantIdInRequestIsEmpty_ThenRewritesAndContinuesPipeline()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns("atenantid");
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };

        var args = new object[]
        {
            new { }, new TestTenantedRequest
            {
                OrganizationId = null
            }
        };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        context.Arguments[1].As<TestTenantedRequest>().OrganizationId.Should().Be("atenantid");
    }

    [Fact]
    public async Task
        WhenInvokeForUnTenantedOrganizationRequestAndTenantIdInRequestIsEmpty_ThenRewritesAndContinuesPipeline()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns("atenantid");
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider.Object
        };

        var args = new object[]
        {
            new { }, new TestUnTenantedOrganizationRequest
            {
                Id = null
            }
        };
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        context.Arguments[1].As<TestUnTenantedOrganizationRequest>().Id.Should().Be("atenantid");
    }

    private class TestUnTenantedRequest : IWebRequest<TestResponse>;

    private class TestTenantedRequest : IWebRequest<TestResponse>, ITenantedRequest
    {
        public string? OrganizationId { get; set; }
    }

    private class TestUnTenantedOrganizationRequest : IWebRequest<TestResponse>, IUnTenantedOrganizationRequest
    {
        public string? Id { get; set; }
    }
}