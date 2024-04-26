using System.IdentityModel.Tokens.Jwt;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class RequestExtensions
{
    /// <summary>
    ///     Returns the ID of the user from the JWT token in the Cookie
    /// </summary>
    public static Result<Optional<string>, Error> GetUserIdFromAuthNCookie(this HttpRequest request)
    {
        var token = GetAuthNCookie(request);
        if (!token.HasValue)
        {
            return Optional<string>.None;
        }

        var userId = GetUserIdClaim(token);
        if (userId.IsFailure)
        {
            return userId.Error;
        }

        return userId.Value.ToOptional();
    }

    private static Optional<string> GetAuthNCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.Token, out var value))
        {
            return value;
        }

        return Optional<string>.None;
    }

    private static Result<string, Error> GetUserIdClaim(string token)
    {
        try
        {
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToArray();
            var userClaim = claims
                .FirstOrDefault(claim => claim.Type == AuthenticationConstants.Claims.ForId);
            if (userClaim.NotExists())
            {
                return Error.ForbiddenAccess(Resources.CSRFMiddleware_InvalidToken);
            }

            return userClaim.Value;
        }
        catch (Exception)
        {
            return Error.ForbiddenAccess(Resources.CSRFMiddleware_InvalidToken);
        }
    }
}