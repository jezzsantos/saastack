using System.IdentityModel.Tokens.Jwt;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class RequestExtensions
{
    /// <summary>
    ///     Returns the ID of the user from the JWT token that is stored in the cookie,
    ///     while the cookie has not expired
    /// </summary>
    public static Result<Optional<string>, Error> GetUserIdFromAuthNCookie(this HttpRequest request)
    {
        var token = GetAuthNCookie(request);
        if (!token.HasValue)
        {
            return Optional<string>.None;
        }

        var userId = GetUserIdClaim(token);
        if (!userId.HasValue)
        {
            return Error.ForbiddenAccess(Resources.RequestExtensions_InvalidToken);
        }
        
        return userId.Value.ToOptional();
    }

    /// <summary>
    ///     Returns the cookie containing the JWT token.
    ///     If the cookie has expired (same expiry as the JWT token) then cookie no longer exists, and this will return None.
    /// </summary>
    private static Optional<string> GetAuthNCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.Token, out var value))
        {
            return value;
        }

        return Optional<string>.None;
    }

    private static Optional<string> GetUserIdClaim(string token)
    {
        try
        {
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToArray();
            var userClaim = claims
                .FirstOrDefault(claim => claim.Type == AuthenticationConstants.Claims.ForId);
            if (userClaim.NotExists())
            {
                return Optional<string>.None;
            }

            return userClaim.Value.ToOptional();
        }
        catch (Exception)
        {
            return Optional<string>.None;
        }
    }
}