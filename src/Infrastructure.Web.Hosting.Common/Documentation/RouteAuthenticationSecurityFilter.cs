using System.Reflection;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Auth;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds the security scheme for each operation,
///     based on the <see cref="AccessType" /> defined in the <see cref="RouteAttribute" />.
/// </summary>
[UsedImplicitly]
public sealed class RouteAuthenticationSecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var type = context.GetRequestType();
        if (!type.HasValue)
        {
            return;
        }

        var requestType = type.Value;
        operation.Security = BuildSecurity(requestType);
    }

    private static List<OpenApiSecurityRequirement> BuildSecurity(Type requestType)
    {
        var routeAttribute = requestType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute.NotExists())
        {
            return [];
        }

        var tokenOrApiKey = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            },
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = APIKeyAuthenticationHandler.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };
        var hmac = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = HMACAuthenticationHandler.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };
        var privateInterHost = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = PrivateInterHostAuthenticationHandler.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };

        return routeAttribute.Access switch
        {
            AccessType.Token => tokenOrApiKey,
            AccessType.HMAC => hmac,
            AccessType.PrivateInterHost => privateInterHost,
            _ => []
        };
    }
}