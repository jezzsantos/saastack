using Common;
using Common.Extensions;
using Infrastructure.WebApi.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Defines an callback that relates a <see cref="PostResult{TResponse}" /> containing a
///     <see cref="TResource" />
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<PostResult<TResponse>, Error> ApiPostResult<TResource, TResponse>()
    where TResource : class where TResponse : IWebResponse;

/// <summary>
///     Defines an callback that relates a <see cref="TResponse" /> containing a
///     <see cref="TResource" />
/// </summary>
// ReSharper disable once UnusedTypeParameter
public delegate Result<TResponse, Error> ApiResult<TResource, TResponse>()
    where TResource : class where TResponse : IWebResponse;

/// <summary>
///     Defines an callback that relates a <see cref="EmptyResponse" />
/// </summary>
public delegate Result<EmptyResponse, Error> ApiEmptyResult();

/// <summary>
///     Provides a container with a <see cref="TResponse" /> and other attributes describing a
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public class PostResult<TResponse>
    where TResponse : IWebResponse
{
    public PostResult(TResponse response, string? location = null)
    {
        Response = response;
        Location = location;
    }

    public string? Location { get; }

    public TResponse Response { get; }

    /// <summary>
    ///     Converts the <see cref="response" /> into a <see cref="PostResult{TResponse}" />
    /// </summary>
    public static implicit operator PostResult<TResponse>(TResponse response)
    {
        return new PostResult<TResponse>(response);
    }
}

public static class HandlerExtensions
{
    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiResult<TResource, TResponse> result,
        WebApiOperation operation)
        where TResource : class where TResponse : IWebResponse
    {
        return result().Match(response => ((PostResult<TResponse>)response.Value).ToResult(operation),
            error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiPostResult<TResource, TResponse> result,
        WebApiOperation operation)
        where TResource : class where TResponse : IWebResponse
    {
        return result().Match(response => response.Value.ToResult(operation), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiEmptyResult result, WebApiOperation operation)
    {
        return result().Match(response => ((PostResult<EmptyResponse>)response.Value).ToResult(operation),
            error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an <see cref="Result{PostResult,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<PostResult<TResponse>, Error> HandleApplicationResult<TResponse, TResource>(
        this Result<TResource, Error> result, Func<TResource, PostResult<TResponse>> onSuccess)
        where TResponse : IWebResponse
    {
        return result.Match(resource => new Result<PostResult<TResponse>, Error>(onSuccess(resource.Value)),
            error => new Result<PostResult<TResponse>, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an <see cref="Result{TResponse,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<TResponse, Error> HandleApplicationResult<TResponse, TResource>(
        this Result<TResource, Error> result, Func<TResource, TResponse> onSuccess)
        where TResponse : IWebResponse
    {
        return result.Match(resource => new Result<TResponse, Error>(onSuccess(resource.Value)),
            error => new Result<TResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{TResource,Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult<TResource>(this Result<TResource, Error> result)
    {
        return result.Match(_ => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult(this Result<Error> result)
    {
        return result.Match(() => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    private static IResult ToResult<TResponse>(this PostResult<TResponse> postResult, WebApiOperation operation)
        where TResponse : IWebResponse
    {
        var response = postResult.Response;
        var location = postResult.Location;
        switch (operation)
        {
            case WebApiOperation.Get:
            case WebApiOperation.Search:
                return Results.Ok(response);
            case WebApiOperation.Post:
            {
                return location.HasValue()
                    ? Results.Created(location!, response)
                    : Results.Ok(response);
            }
            case WebApiOperation.PutPatch:
                return Results.Accepted(null, response);
            case WebApiOperation.Delete:
                return Results.NoContent();
            default:
                return Results.Ok(response);
        }
    }

    private static IResult ToResult(this Error error)
    {
        var httpError = error.ToHttpError();
        return Results.Problem(statusCode: (int)httpError.Code, detail: httpError.Message);
    }
}