using System.Reflection;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Infrastructure.Common;

/// <summary>
///     Provides a factory for rehydrating domain objects from persistence
/// </summary>
public class DomainFactory : IDomainFactory
{
    private const string FactoryMethodName = nameof(IRehydratableObject.Rehydrate);
    private readonly Dictionary<Type, AggregateRootFactory<IDehydratableAggregateRoot>> _aggregateRootFactories;
    private readonly IDependencyContainer _container;
    private readonly Dictionary<Type, EntityFactory<IDehydratableEntity>> _persistableEntityFactories;
    private readonly Dictionary<Type, ValueObjectFactory<IDehydratableValueObject>> _valueObjectFactories;

    public DomainFactory(IDependencyContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        _container = container;
        _aggregateRootFactories = new Dictionary<Type, AggregateRootFactory<IDehydratableAggregateRoot>>();
        _persistableEntityFactories =
            new Dictionary<Type, EntityFactory<IDehydratableEntity>>();
        NonDehydratableEntityFactories = new List<Type>();
        _valueObjectFactories = new Dictionary<Type, ValueObjectFactory<IDehydratableValueObject>>();
    }

    public IReadOnlyDictionary<Type, AggregateRootFactory<IDehydratableAggregateRoot>> AggregateRootFactories =>
        _aggregateRootFactories;

    public IReadOnlyDictionary<Type, EntityFactory<IDehydratableEntity>>
        EntityFactories => _persistableEntityFactories;

    // ReSharper disable once CollectionNeverQueried.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public List<Type> NonDehydratableEntityFactories { get; }

    public IReadOnlyDictionary<Type, ValueObjectFactory<IDehydratableValueObject>> ValueObjectFactories =>
        _valueObjectFactories;

    public IDehydratableAggregateRoot RehydrateAggregateRoot(Type aggregateType,
        HydrationProperties rehydratingPropertyValues)
    {
        ArgumentNullException.ThrowIfNull(rehydratingPropertyValues);

        if (!_aggregateRootFactories.ContainsKey(aggregateType))
        {
            throw new InvalidOperationException(
                Resources.DomainFactory_AggregateTypeNotFound.Format(aggregateType.Name));
        }

        var identifier =
            rehydratingPropertyValues.GetValueOrDefault<Identifier>(nameof(IIdentifiableEntity.Id));
        var factory = _aggregateRootFactories[aggregateType];
        return factory(identifier.Value, _container, rehydratingPropertyValues);
    }

    public IDehydratableEntity RehydrateEntity(Type entityType,
        HydrationProperties rehydratingPropertyValues)
    {
        ArgumentNullException.ThrowIfNull(rehydratingPropertyValues);

        var baseEntityType = GetBaseType(entityType);
        if (!_persistableEntityFactories.ContainsKey(baseEntityType))
        {
            throw new InvalidOperationException(Resources.DomainFactory_EntityTypeNotFound.Format(entityType.Name));
        }

        var identifier =
            rehydratingPropertyValues.GetValueOrDefault<Identifier>(nameof(IIdentifiableEntity.Id));
        var factory = _persistableEntityFactories[baseEntityType];
        return factory(identifier.Value, _container, rehydratingPropertyValues);
    }

    public IDehydratableValueObject RehydrateValueObject(Type valueObjectType, string rehydratingPropertyValue)
    {
        ArgumentNullException.ThrowIfNull(valueObjectType);
        ArgumentNullException.ThrowIfNull(rehydratingPropertyValue);

        var baseValueObjectType = GetBaseType(valueObjectType);
        if (!_valueObjectFactories.ContainsKey(baseValueObjectType))
        {
            throw new InvalidOperationException(
                Resources.DomainFactory_ValueObjectTypeNotFound.Format(valueObjectType.Name));
        }

        return _valueObjectFactories[baseValueObjectType](rehydratingPropertyValue, _container);
    }

    [AssertionMethod]
    public void RegisterDomainTypesFromAssemblies(params Assembly[] assembliesContainingFactories)
    {
        ArgumentNullException.ThrowIfNull(assembliesContainingFactories);
        if (assembliesContainingFactories.Length <= 0)
        {
            return;
        }

        foreach (var assembly in assembliesContainingFactories)
        {
            var domainTypes = assembly.GetTypes()
                .Where(t => IsEventingAggregateRoot(t) || IsEventingEntity(t) || IsDehydratableEntityOrAggregate(t)
                            || IsValueObject(t))
                .ToList();
            foreach (var type in domainTypes)
            {
                if (IsEventingAggregateRoot(type))
                {
                    var factoryMethod = GetAggregateRootFactoryMethod(type);
                    var @delegate = (AggregateRootFactory<IDehydratableAggregateRoot>)
                        CheckMethodSignatureAndInvoke(factoryMethod, type);
                    _aggregateRootFactories[type] = @delegate;
                }
                else if (IsEventingEntity(type))
                {
                    NonDehydratableEntityFactories.Add(type);
                }
                else if (IsDehydratableEntityOrAggregate(type))
                {
                    var factoryMethod = GetEntityFactoryMethod(type);
                    var @delegate = (EntityFactory<IDehydratableEntity>)
                        CheckMethodSignatureAndInvoke(factoryMethod, type);
                    _persistableEntityFactories[type] = @delegate;
                }
                else if (IsValueObject(type))
                {
                    var factoryMethod = GetValueObjectFactoryMethod(type);
                    var @delegate = (ValueObjectFactory<IDehydratableValueObject>)
                        CheckMethodSignatureAndInvoke(factoryMethod, type);
                    _valueObjectFactories[type] = @delegate;
                }
            }
        }

        return;

        object CheckMethodSignatureAndInvoke(MethodInfo? methodInfo, Type type)
        {
            if (methodInfo.NotExists())
            {
                throw new InvalidOperationException(
                    Resources.DomainFactory_AggregateRootFactoryMethodNotFound.Format(type.Name,
                        FactoryMethodName));
            }

            if (IsWrongNamedOrHasParameters(methodInfo))
            {
                throw new InvalidOperationException(
                    Resources.DomainFactory_FactoryMethodHasParameters.Format(type.Name,
                        methodInfo.Name, FactoryMethodName));
            }

            return methodInfo.Invoke(null, null)!;
        }
    }

    public static DomainFactory CreateRegistered(IDependencyContainer container,
        params Assembly[] assembliesContainingFactories)
    {
        var domainFactory = new DomainFactory(container);
        domainFactory.RegisterDomainTypesFromAssemblies(assembliesContainingFactories);
        return domainFactory;
    }

    private static Type GetBaseType(Type type)
    {
        if (!Optional.IsOptionalType(type))
        {
            return type;
        }

        if (Optional.TryGetContainedType(type, out var optionalValueObjectType))
        {
            return optionalValueObjectType!;
        }

        return type;
    }

    private static bool IsWrongNamedOrHasParameters(MethodBase method)
    {
        return method.Name.NotEqualsOrdinal(FactoryMethodName)
               || method.GetParameters().Length != 0;
    }

    private static bool IsEventingAggregateRoot(Type type)
    {
        return !type.IsAbstract && typeof(IEventingAggregateRoot).IsAssignableFrom(type);
    }

    private static bool IsEventingEntity(Type type)
    {
        return !type.IsAbstract && typeof(IEventingEntity).IsAssignableFrom(type);
    }

    private static bool IsDehydratableEntityOrAggregate(Type type)
    {
        return !type.IsAbstract && typeof(IDehydratableEntity).IsAssignableFrom(type);
    }

    private static MethodInfo? GetAggregateRootFactoryMethod(Type type)
    {
        return type
            .GetMethods()
            .FirstOrDefault(method => method.IsStatic
                                      && method.IsPublic
                                      && method.ReturnType != typeof(void)
                                      && method.ReturnType.BaseType == typeof(MulticastDelegate)
                                      && method.ReturnType.IsGenericType
                                      && method.ReturnType.GenericTypeArguments.Any()
                                      && typeof(IEventingAggregateRoot).IsAssignableFrom(
                                          method.ReturnType.GenericTypeArguments[0])
                                      && method.ReturnType.GetGenericTypeDefinition()
                                          .IsAssignableFrom(typeof(AggregateRootFactory<>)));
    }

    private static MethodInfo? GetEntityFactoryMethod(Type type)
    {
        return type
            .GetMethods()
            .FirstOrDefault(method => method.IsStatic
                                      && method.IsPublic
                                      && method.ReturnType != typeof(void)
                                      && method.ReturnType.BaseType == typeof(MulticastDelegate)
                                      && method.ReturnType.IsGenericType
                                      && method.ReturnType.GenericTypeArguments.Any()
                                      && typeof(IDehydratableEntity).IsAssignableFrom(
                                          method.ReturnType.GenericTypeArguments[0])
                                      && method.ReturnType.GetGenericTypeDefinition()
                                          .IsAssignableFrom(typeof(EntityFactory<>)));
    }

    private static bool IsValueObject(Type type)
    {
        return !type.IsAbstract
               && type.IsSubclassOfRawGeneric(typeof(ValueObjectBase<>));
    }

    private static MethodInfo? GetValueObjectFactoryMethod(Type type)
    {
        return type
            .GetMethods()
            .FirstOrDefault(method => method.IsStatic
                                      && method.IsPublic
                                      && method.ReturnType != typeof(void)
                                      && method.ReturnType.BaseType == typeof(MulticastDelegate)
                                      && method.ReturnType.IsGenericType
                                      && method.ReturnType.GenericTypeArguments.Any()
                                      && method.ReturnType.GenericTypeArguments[0]
                                          .IsSubclassOfRawGeneric(typeof(ValueObjectBase<>))
                                      && method.ReturnType.GetGenericTypeDefinition()
                                          .IsAssignableFrom(typeof(ValueObjectFactory<>)));
    }
}

internal static class ReflectionExtensions
{
    public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
    {
        while (toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType
                ? toCheck.GetGenericTypeDefinition()
                : toCheck;
            if (generic == cur)
            {
                return true;
            }

            toCheck = toCheck.BaseType!;
        }

        return false;
    }
}