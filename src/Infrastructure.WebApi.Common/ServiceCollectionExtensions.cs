using System.Reflection;
using Common.Extensions;
using FluentValidation;
using Infrastructure.WebApi.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <see cref="ValidationBehavior{TRequest,TResponse}" /> for every <see cref="IWebRequest{TResponse}" />
    ///     found in the specified <see cref="assembliesContainingApis" /> that has a corresponding registered
    ///     <see cref="IValidator{TRequest}" />
    /// </summary>
    public static MediatRServiceConfiguration AddValidatorBehaviors(this MediatRServiceConfiguration configuration,
        IEnumerable<Type> registeredValidators, IEnumerable<Assembly> assembliesContainingApis)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(registeredValidators);
        ArgumentNullException.ThrowIfNull(assembliesContainingApis);

        var validators = registeredValidators.ToList();
        if (validators.HasNone())
        {
            return configuration;
        }

        var serviceClasses = assembliesContainingApis.SelectMany(assembly => assembly.GetTypes())
            .Where(IsServiceClass);

        var requestTypes = serviceClasses
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Where(method => method is { IsAbstract: false })
            .SelectMany(method =>
            {
                return method.GetParameters()
                    .Where(parameter => IsRequestType(parameter.ParameterType))
                    .Select(parameter => parameter.ParameterType);
            })
            .Distinct()
            .ToList();

        foreach (var requestType in requestTypes)
        {
            var requestValidator = FindValidatorOfRequestType(validators, requestType);
            if (requestValidator is null)
            {
                continue;
            }

            var responseType = GetResponseType(requestType);
            if (responseType is null)
            {
                continue;
            }

            var template1 = typeof(IPipelineBehavior<,>);
            var behaviorType = template1.MakeGenericType(requestType, typeof(IResult));

            var template2 = typeof(ValidationBehavior<,>);
            var behaviorInstance = template2.MakeGenericType(requestType, responseType);

            configuration.AddBehavior(behaviorType, behaviorInstance, ServiceLifetime.Scoped);
        }

        return configuration;

        static Type? FindValidatorOfRequestType(List<Type> validators, Type requestType)
        {
            return validators.Find(type =>
            {
                var implementedInterfaces = type.GetInterfaces();
                var validatorInterface = implementedInterfaces.FirstOrDefault(@interface =>
                    @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>));
                if (validatorInterface is not null)
                {
                    var validatorRequestType = validatorInterface.GenericTypeArguments.FirstOrDefault();
                    if (validatorRequestType is not null)
                    {
                        return validatorRequestType == requestType;
                    }
                }

                return false;
            });
        }

        static bool IsServiceClass(Type type)
        {
            return type is { IsAbstract: false, IsGenericTypeDefinition: false }
                   && type.IsAssignableTo(typeof(IWebApiService));
        }

        static bool IsRequestType(Type type)
        {
            return GetRequestType(type) is not null;
        }

        static Type? GetRequestType(Type type)
        {
            var interfaces = type.GetInterfaces();
            return interfaces.FirstOrDefault(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IWebRequest<>));
        }

        static Type? GetResponseType(Type type)
        {
            var requestType = GetRequestType(type);
            if (requestType is not null)
            {
                return requestType.GenericTypeArguments.FirstOrDefault();
            }

            return null;
        }
    }

    /// <summary>
    ///     Registers every <see cref="IValidator{TRequest}" /> found in the specified
    ///     <see cref="assembliesContainingValidators" />
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection RegisterValidators(this IServiceCollection services,
        IEnumerable<Assembly> assembliesContainingValidators, out IEnumerable<Type> registeredValidators)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembliesContainingValidators);

        var validators = new List<Type>();
        AssemblyScanner.FindValidatorsInAssemblies(assembliesContainingValidators)
            .ForEach(result =>
            {
                services.AddScoped(result.InterfaceType, result.ValidatorType);
                validators.Add(result.ValidatorType);
            });

        registeredValidators = validators;
        return services;
    }
}