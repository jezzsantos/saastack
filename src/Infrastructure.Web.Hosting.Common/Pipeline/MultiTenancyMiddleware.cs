using System.Reflection;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Provides middleware to detect the tenant of incoming requests.
///     Detects the current tenant using the <see cref="ITenantDetective" />,
///     and if required and missing, then extracts the "DefaultOrganizationId" from the authenticated user
///     and sets the <see cref="ITenancyContext.Current" /> tenant.
///     Downstream, an endpoint filter will rewrite the required, missing tenant into the request
/// </summary>
public class MultiTenancyMiddleware
{
    private readonly IEndUsersService _endUsersService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly RequestDelegate _next;
    private readonly IOrganizationsService _organizationsService;

    public MultiTenancyMiddleware(RequestDelegate next, IIdentifierFactory identifierFactory,
        IEndUsersService endUsersService, IOrganizationsService organizationsService)
    {
        _next = next;
        _identifierFactory = identifierFactory;
        _endUsersService = endUsersService;
        _organizationsService = organizationsService;
    }

    public async Task InvokeAsync(HttpContext context, ITenancyContext tenancyContext,
        ICallerContextFactory callerContextFactory, ITenantDetective tenantDetective)
    {
        var caller = callerContextFactory.Create();
        var cancellationToken = context.RequestAborted;

        var result = await VerifyRequestAsync(caller, context, tenancyContext, tenantDetective, cancellationToken);
        if (!result.IsSuccessful)
        {
            var httpError = result.Error.ToHttpError();
            var details = Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
            await details
                .ExecuteAsync(context);
            return;
        }

        await _next(context); //Continue down the pipeline
    }

    private async Task<Result<Error>> VerifyRequestAsync(ICallerContext caller, HttpContext httpContext,
        ITenancyContext tenancyContext, ITenantDetective tenantDetective, CancellationToken cancellationToken)
    {
        var requestDtoType = GetRequestDtoType(httpContext);
        var detected = await tenantDetective.DetectTenantAsync(httpContext, requestDtoType, cancellationToken);
        if (!detected.IsSuccessful)
        {
            return detected.Error;
        }

        List<Membership>? memberships = null;
        var detectedResult = detected.Value;
        var tenantId = detectedResult.TenantId.ValueOrDefault;
        if (MissingRequiredTenantIdFromRequest(detectedResult))
        {
            var defaultOrganizationId =
                await VerifyDefaultOrganizationIdForCallerAsync(caller, memberships, cancellationToken);
            if (!defaultOrganizationId.IsSuccessful)
            {
                return defaultOrganizationId.Error;
            }

            if (defaultOrganizationId.Value.HasValue())
            {
                tenantId = defaultOrganizationId.Value;
            }
        }

        if (tenantId.HasNoValue())
        {
            return Result.Ok;
        }

        var isMember = await VerifyCallerMembershipAsync(caller, memberships, tenantId, cancellationToken);
        if (!isMember.IsSuccessful)
        {
            return isMember.Error;
        }

        var set = await SetTenantIdAsync(caller, _identifierFactory, tenancyContext, _organizationsService, tenantId,
            cancellationToken);
        return set.IsSuccessful
            ? Result.Ok
            : set.Error;
    }

    private static bool MissingRequiredTenantIdFromRequest(TenantDetectionResult detectedResult)
    {
        return detectedResult.ShouldHaveTenantId && detectedResult.TenantId.ValueOrDefault.HasNoValue();
    }

    private async Task<Result<string?, Error>> VerifyDefaultOrganizationIdForCallerAsync(ICallerContext caller,
        List<Membership>? memberships, CancellationToken cancellationToken)
    {
        if (memberships.NotExists())
        {
            var retrievedMemberships = await GetMembershipsForCallerAsync(caller, cancellationToken);
            if (!retrievedMemberships.IsSuccessful)
            {
                return retrievedMemberships.Error;
            }

            memberships = retrievedMemberships.Value;
        }

        var defaultOrganizationId = GetDefaultOrganizationId(memberships);
        if (defaultOrganizationId.HasValue())
        {
            return defaultOrganizationId;
        }

        return Error.Validation(Resources.MultiTenancyMiddleware_MissingTenantId);
    }

    private async Task<Result<Error>> VerifyCallerMembershipAsync(ICallerContext caller, List<Membership>? memberships,
        string tenantId, CancellationToken cancellationToken)
    {
        if (!IsTenantedUser(caller))
        {
            return Result.Ok;
        }

        if (memberships.NotExists())
        {
            var retrievedMemberships = await GetMembershipsForCallerAsync(caller, cancellationToken);
            if (!retrievedMemberships.IsSuccessful)
            {
                return retrievedMemberships.Error;
            }

            memberships = retrievedMemberships.Value;
        }

        if (IsMemberOfOrganization(memberships, tenantId))
        {
            return Result.Ok;
        }

        return Error.ForbiddenAccess(Resources.MultiTenancyMiddleware_UserNotAMember.Format(tenantId));
    }

    /// <summary>
    ///     Validates the tenant ID and sets it in the <see cref="ITenancyContext" />,
    ///     and if necessary updates the request DTO with the tenant ID
    /// </summary>
    private static async Task<Result<Error>> SetTenantIdAsync(ICallerContext caller,
        IIdentifierFactory identifierFactory, ITenancyContext tenancyContext,
        IOrganizationsService organizationsService, string tenantId, CancellationToken cancellationToken)
    {
        var isValid = IsTenantIdValid(identifierFactory, tenantId);
        if (!isValid)
        {
            return Error.Validation(Resources.MultiTenancyMiddleware_InvalidTenantId);
        }

        var settings = await organizationsService.GetSettingsPrivateAsync(caller, tenantId, cancellationToken);
        if (!settings.IsSuccessful)
        {
            return settings.Error;
        }

        tenancyContext.Set(tenantId, settings.Value);

        return Result.Ok;
    }

    private static Optional<Type> GetRequestDtoType(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint.NotExists())
        {
            return Optional<Type>.None;
        }

        var method = endpoint.Metadata.GetMetadata<MethodInfo>();
        if (method.NotExists())
        {
            return Optional<Type>.None;
        }

        var args = method.GetParameters();
        if (args.Length < 2)
        {
            return Optional<Type>.None;
        }

        var requestDtoType = args[1].ParameterType;
        if (!requestDtoType.IsAssignableTo(typeof(IWebRequest)))
        {
            return Optional<Type>.None;
        }

        return requestDtoType;
    }

    private async Task<Result<List<Membership>, Error>> GetMembershipsForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        if (!IsTenantedUser(caller))
        {
            return new List<Membership>();
        }

        var memberships = await _endUsersService.GetMembershipsPrivateAsync(caller, caller.CallerId, cancellationToken);
        if (!memberships.IsSuccessful)
        {
            return memberships.Error;
        }

        return memberships.Value.Memberships;
    }

    private static bool IsTenantedUser(ICallerContext caller)
    {
        if (!caller.IsAuthenticated)
        {
            return false;
        }

        return !caller.IsServiceAccount;
    }

    private static string? GetDefaultOrganizationId(List<Membership> memberships)
    {
        var defaultOrganization = memberships.FirstOrDefault(ms => ms.IsDefault);
        if (defaultOrganization.Exists())
        {
            return defaultOrganization.OrganizationId;
        }

        return null;
    }

    private static bool IsMemberOfOrganization(List<Membership> memberships, string tenantId)
    {
        if (memberships.HasNone())
        {
            return false;
        }

        return memberships.Any(ms => ms.OrganizationId == tenantId);
    }

    private static bool IsTenantIdValid(IIdentifierFactory identifierFactory, string tenantId)
    {
        return identifierFactory.IsValid(tenantId.ToId());
    }
}