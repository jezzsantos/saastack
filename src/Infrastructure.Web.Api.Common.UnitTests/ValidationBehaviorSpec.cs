using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Infrastructure.Web.Api.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[Trait("Category", "Unit")]
public class ValidationBehaviorSpec
{
    private readonly ValidationBehavior<TestRequest, TestResponse> _behavior;
    private readonly Mock<IValidator<TestRequest>> _validator;

    public ValidationBehaviorSpec()
    {
        _validator = new Mock<IValidator<TestRequest>>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request.Scheme)
            .Returns("ascheme");
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request.Host)
            .Returns(new HostString("ahost"));
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request.PathBase)
            .Returns(new PathString("/abasepath"));
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request.Path)
            .Returns("/apath");
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request.QueryString)
            .Returns(new QueryString("?aquerystring"));
        _behavior = new ValidationBehavior<TestRequest, TestResponse>(_validator.Object, httpContextAccessor.Object);
    }

    [Fact]
    public async Task WhenHandleAndValidatorPasses_ThenExecutesMiddleware()
    {
        var request = new TestRequest();
        var wasNextCalled = false;
        _validator.Setup(val => val.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ValidationResult()));

        var result = await _behavior.Handle(request, () =>
        {
            wasNextCalled = true;
            return Task.FromResult(Results.Ok());
        }, CancellationToken.None);

        wasNextCalled.Should().BeTrue();
        _validator.Verify(val => val.ValidateAsync(request, CancellationToken.None));
        result.Should().Be(Results.Ok());
    }

    [Fact]
    public async Task WhenHandleAndValidatorFails_ThenReturnsBadRequest()
    {
        var request = new TestRequest();
        var wasNextCalled = false;
        var errors = new ValidationResult(new List<ValidationFailure> { new("aproperty", "anerror") });
        _validator.Setup(val => val.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(errors));

        var result = await _behavior.Handle(request, () =>
        {
            wasNextCalled = true;
            return Task.FromResult(Results.Ok());
        }, CancellationToken.None);

        wasNextCalled.Should().BeFalse();
        _validator.Verify(val => val.ValidateAsync(request, CancellationToken.None));
        result.Should()
            .BeEquivalentTo(TypedResults.Problem(errors.ToRfc7807("ascheme://ahost/abasepath/apath?aquerystring")));
    }
}