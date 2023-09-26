using Common;
using Infrastructure.WebApi.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Defines an API result that relates a <see cref="TResponse" /> containing a
///     <see cref="TResource" />
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiResult<TResource, TResponse>() where TResource : class;

public static class HandlerExtensions
{
    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiResult<TResource, TResponse> result)
        where TResource : class
    {
        return result().Match(
            Results.Ok,
            error =>
            {
                var httpError = error.ToHttpError();
                return Results.Problem(statusCode: (int)httpError.Code,
                    detail: httpError.Message);
            });
    }

    /// <summary>
    ///     Converts the value in the <see cref="result" /> from a <see cref="TResource" /> to an <see cref="TResponse" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<TResponse, Error> HandleApplicationResult<TResponse, TResource>(
        this Result<TResource, Error> result, Func<TResource, TResponse> onSuccess)
    {
        return result.Match(
            resource => new Result<TResponse, Error>(onSuccess(resource)),
            error => new Result<TResponse, Error>(error));
    }
}