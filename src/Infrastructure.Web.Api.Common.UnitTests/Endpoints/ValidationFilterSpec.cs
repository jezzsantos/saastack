using System.Net;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public class ValidationFilterSpec
{
    private readonly ValidationFilter<TestRequest> _filter;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<EndpointFilterDelegate> _next;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IValidator<TestRequest>> _validator;

    public ValidationFilterSpec()
    {
        _validator = new Mock<IValidator<TestRequest>>();
        _serviceProvider = new Mock<IServiceProvider>();
        _httpContext = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "ascheme",
                Host = new HostString("ahost"),
                PathBase = new PathString("/abasepath"),
                Path = "/apath",
                QueryString = new QueryString("?aquerystring")
            },
            RequestServices = _serviceProvider.Object
        };
        _next = new Mock<EndpointFilterDelegate>();

        _filter = new ValidationFilter<TestRequest>();
    }

    [Fact]
    public async Task WhenInvokeAndNoRequestDto_ThenDoesNothing()
    {
        var args = new object[] { "anarg1", "anarg2" };
        var context = new DefaultEndpointFilterInvocationContext(_httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        _serviceProvider.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAndRequestTypeIsWrongType_ThenDoesNothing()
    {
        var args = new object[] { "anarg1", new HealthCheckRequest() };
        var context = new DefaultEndpointFilterInvocationContext(_httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _next.Verify(n => n.Invoke(context));
        _serviceProvider.Verify(sp => sp.GetService(It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAndNoValidatorRegistered_ThenDoesNothing()
    {
        var args = new object[] { "anarg1", new TestRequest() };
        var context = new DefaultEndpointFilterInvocationContext(_httpContext, args);

        await _filter.InvokeAsync(context, _next.Object);

        _serviceProvider.Verify(sp => sp.GetService(It.Is<Type>(t => t == typeof(IValidator<TestRequest>))));
        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAndValidatorSucceeds_ThenValidatesAndContinues()
    {
        var requestDto = new TestRequest();
        var args = new object[] { "anarg1", requestDto };
        var context = new DefaultEndpointFilterInvocationContext(_httpContext, args);
        _validator.Setup(val => val.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _serviceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(_validator.Object);

        await _filter.InvokeAsync(context, _next.Object);

        _serviceProvider.Verify(sp => sp.GetService(It.Is<Type>(t => t == typeof(IValidator<TestRequest>))));
        _validator.Verify(val => val.ValidateAsync(requestDto, It.IsAny<CancellationToken>()));
        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAndValidatorFails_ThenRespondsWithValidationError()
    {
        var requestDto = new TestRequest();
        var args = new object[] { "anarg1", requestDto };
        var context = new DefaultEndpointFilterInvocationContext(_httpContext, args);
        var errors = new ValidationResult(new List<ValidationFailure> { new("aproperty", "anerror") });
        _validator.Setup(val => val.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);
        _serviceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(_validator.Object);

        var result = await _filter.InvokeAsync(context, _next.Object);

        result.Should().BeOfType<ProblemHttpResult>();
        result.As<ProblemHttpResult>().StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        result.As<ProblemHttpResult>().Should()
            .BeEquivalentTo(TypedResults.Problem(errors.ToRfc7807("ascheme://ahost/abasepath/apath?aquerystring")));
        _serviceProvider.Verify(sp => sp.GetService(It.Is<Type>(t => t == typeof(IValidator<TestRequest>))));
        _validator.Verify(val => val.ValidateAsync(requestDto, It.IsAny<CancellationToken>()));
        _next.Verify(n => n.Invoke(context), Times.Never);
    }
}