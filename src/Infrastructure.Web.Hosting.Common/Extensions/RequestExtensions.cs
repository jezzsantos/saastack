using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class RequestExtensions
{
    /// <summary>
    ///     Returns the claims from the JWT token that is stored in the cookie,
    /// </summary>
    public static Claim[] GetClaimsFromAuthNCookie(this HttpRequest request)
    {
        var token = TokenFromAuthNCookie(request);
        if (!token.HasValue)
        {
            return [];
        }

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);
            return jwtToken.Claims.ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    ///     Returns the ID of the user from the JWT token that is stored in the cookie,
    ///     while the cookie has not expired
    /// </summary>
    public static Result<Optional<string>, Error> GetUserIdFromAuthNCookie(this HttpRequest request)
    {
        var token = TokenFromAuthNCookie(request);
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
    public static Optional<string> TokenFromAuthNCookie(this HttpRequest request)
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