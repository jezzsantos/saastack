using System.Reflection;
using FluentAssertions;
using FluentValidation;
using Infrastructure.WebApi.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common.UnitTests;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsSpec
{
    [Fact]
    public void WhenRegisterValidators_ThenRegistersInContainer()
    {
        var services = new ServiceCollection();

        services.RegisterValidators(new[] { typeof(ServiceCollectionExtensionsSpec).Assembly }, out var validators);

        services.Should().ContainSingle(service => service.ImplementationType == typeof(TestRequestValidator));
    }

    [Fact]
    public void WhenAddValidatorBehaviorsAndNoRegisteredValidators_ThenRegistersNoBehaviors()
    {
        var configuration = new MediatRServiceConfiguration();
        var assemblies = new[] { typeof(ServiceCollectionExtensionsSpec).Assembly };

        configuration.AddValidatorBehaviors(Enumerable.Empty<Type>(), assemblies);

        configuration.BehaviorsToRegister.Should().BeEmpty();
    }


    [Fact]
    public void WhenAddValidatorBehaviorsAndNoRequestTypes_ThenRegistersNoBehaviors()
    {
        var configuration = new MediatRServiceConfiguration();
        var validators = new[] { typeof(TestRequestValidator) };

        configuration.AddValidatorBehaviors(validators, Enumerable.Empty<Assembly>());

        configuration.BehaviorsToRegister.Should().BeEmpty();
    }


    [Fact]
    public void WhenAddValidatorBehaviorsAndNoMatchingValidators_ThenRegistersNoBehaviors()
    {
        var configuration = new MediatRServiceConfiguration();
        var validators = new[] { typeof(TestRequestValidator2) };
        var assemblies = new[] { typeof(ServiceCollectionExtensionsSpec).Assembly };

        configuration.AddValidatorBehaviors(validators, assemblies);

        configuration.BehaviorsToRegister.Should().BeEmpty();
    }

    [Fact]
    public void WhenAddValidatorBehaviors_ThenRegistersBehavior()
    {
        var configuration = new MediatRServiceConfiguration();
        var validators = new[] { typeof(TestRequestValidator) };
        var assemblies = new[] { typeof(ServiceCollectionExtensionsSpec).Assembly };

        configuration.AddValidatorBehaviors(validators, assemblies);

        var expectedBehaviorImplementationType =
            typeof(ValidationBehavior<,>).MakeGenericType(typeof(TestRequest), typeof(TestResponse));
        configuration.BehaviorsToRegister.Should()
            .ContainSingle(behavior => behavior.ImplementationType == expectedBehaviorImplementationType);
    }

    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
    }

    public class TestRequestValidator2 : AbstractValidator<TestRequest2>
    {
    }

    public class TestApiWithoutMethods : IWebApiService
    {
    }

    public class TestApi : IWebApiService
    {
        [WebApiRoute("/aroute", WebApiOperation.Get)]
        public Task<IResult> Get(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Results.Ok("amessage"));
        }
    }

    public class TestRequest2 : IWebRequest<TestResponse>
    {
    }
}