using Application.Interfaces;
using Application.Interfaces.Extensions;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     This middleware is responsible for verifying that a tenant ID is set in the request context,
///     and that the caller is a member of that specific Organization.
///     Detects the current tenant using the <see cref="ITenantDetective" />,
///     and if the request type is deemed "tenanted", but the tenant ID is missing,
///     then this middleware extracts the "DefaultOrganizationId" from the authenticated user
///     and sets the value or <see cref="ITenancyContext.Current" /> to that tenant.
///     Note: Downstream, another minimal endpoint filter will rewrite the missing tenant ID into the request DTO
/// </summary>
public class MultiTenancyMiddleware
{
    private readonly IIdentifierFactory _identifierFactory;
    private readonly RequestDelegate _next;

    public MultiTenancyMiddleware(RequestDelegate next, IIdentifierFactory identifierFactory)
    {
        _next = next;
        _identifierFactory = identifierFactory;
    }

    public async Task InvokeAsync(HttpContext context, ITenancyContext tenancyContext,
        ICallerContextFactory callerContextFactory, ITenantDetective tenantDetective, IEndUsersService endUsersService,
        IOrganizationsService organizationsService)
    {
        var caller = callerContextFactory.Create();
        var cancellationToken = context.RequestAborted;

        var result = await VerifyRequestAsync(caller, context, tenancyContext, tenantDetective, endUsersService,
            organizationsService, cancellationToken);
        if (result.IsFailure)
        {
            var details = result.Error.ToProblem();
            await details
                .ExecuteAsync(context);
            return;
        }

        await _next(context); //Continue down the pipeline
    }

    private async Task<Result<Error>> VerifyRequestAsync(ICallerContext caller, HttpContext httpContext,
        ITenancyContext tenancyContext, ITenantDetective tenantDetective, IEndUsersService endUsersService,
        IOrganizationsService organizationsService, CancellationToken cancellationToken)
    {
        var requestDtoType = httpContext.GetRequestDtoType();
        var detected = await tenantDetective.DetectTenantAsync(httpContext, requestDtoType.Exists()
            ? requestDtoType
            : Optional<Type>.None, cancellationToken);
        if (detected.IsFailure)
        {
            return detected.Error;
        }

        List<Membership>? memberships = null;
        var detectedResult = detected.Value;
        var tenantId = detectedResult.TenantId.ValueOrDefault;
        if (MissingRequiredTenantIdFromRequest(detectedResult))
        {
            var defaultOrganizationId =
                await VerifyDefaultOrganizationIdForCallerAsync(caller, endUsersService, memberships,
                    cancellationToken);
            if (defaultOrganizationId.IsFailure)
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

        if (detectedResult.ShouldHaveTenantId
            || !caller.IsOperations())
        {
            var verifiedMember =
                await VerifyCallerMembershipAsync(caller, endUsersService, memberships, tenantId, cancellationToken);
            if (verifiedMember.IsFailure)
            {
                return verifiedMember.Error;
            }
        }

        var set = await SetTenantIdAsync(caller, _identifierFactory, tenancyContext, organizationsService, tenantId,
            cancellationToken);
        return set.IsSuccessful
            ? Result.Ok
            : set.Error;
    }

    private static bool MissingRequiredTenantIdFromRequest(TenantDetectionResult detectedResult)
    {
        return detectedResult.ShouldHaveTenantId && detectedResult.TenantId.ValueOrDefault.HasNoValue();
    }

    private static async Task<Result<string?, Error>> VerifyDefaultOrganizationIdForCallerAsync(ICallerContext caller,
        IEndUsersService endUsersService, List<Membership>? memberships, CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            return Error.Validation(Resources.MultiTenancyMiddleware_MissingDefaultOrganization);
        }

        if (memberships.NotExists())
        {
            var retrievedMemberships = await GetMembershipsForCallerAsync(caller, endUsersService, cancellationToken);
            if (retrievedMemberships.IsFailure)
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

        return Error.Validation(Resources.MultiTenancyMiddleware_MissingDefaultOrganization);
    }

    private static async Task<Result<Error>> VerifyCallerMembershipAsync(ICallerContext caller,
        IEndUsersService endUsersService, List<Membership>? memberships,
        string tenantId, CancellationToken cancellationToken)
    {
        if (!IsTenantedUser(caller))
        {
            return Result.Ok;
        }

        if (memberships.NotExists())
        {
            var retrievedMemberships = await GetMembershipsForCallerAsync(caller, endUsersService, cancellationToken);
            if (retrievedMemberships.IsFailure)
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
        if (settings.IsFailure)
        {
            return settings.Error;
        }

        tenancyContext.Set(tenantId, settings.Value);

        return Result.Ok;
    }

    private static async Task<Result<List<Membership>, Error>> GetMembershipsForCallerAsync(ICallerContext caller,
        IEndUsersService endUsersService, CancellationToken cancellationToken)
    {
        if (!IsTenantedUser(caller))
        {
            return new List<Membership>();
        }

        var memberships = await endUsersService.GetMembershipsPrivateAsync(caller, caller.CallerId, cancellationToken);
        if (memberships.IsFailure)
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