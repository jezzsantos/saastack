#if TESTINGONLY
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;

namespace ApiHost1.Api.TestingOnly;

public sealed class TestingWebApi : IWebApiService
{
    private static List<Type>? _allRepositories;
    private static IReadOnlyList<IApplicationRepository>? _repositories;
    private readonly ICallerContextFactory _callerFactory;
    private readonly IServiceProvider _serviceProvider;

    public TestingWebApi(ICallerContextFactory callerFactory, IServiceProvider serviceProvider)
    {
        _callerFactory = callerFactory;
        _serviceProvider = serviceProvider;
    }

    // ReSharper disable once InconsistentNaming
    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthNHMAC(
        GetCallerWithHMACTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthNToken(
        GetCallerWithTokenOrAPIKeyTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZAnonymous(
        AuthorizeByNothingTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZByFeature(
        AuthorizeByFeatureTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, GetCallerTestingOnlyResponse>> AuthZByRole(
        AuthorizeByRoleTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<GetCallerTestingOnlyResponse, Error>(new GetCallerTestingOnlyResponse
            { CallerId = _callerFactory.Create().CallerId });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ContentNegotiationGet(
        ContentNegotiationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    public async Task<ApiEmptyResult> DestroyAllRepositories(DestroyAllRepositoriesRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var repositoryTypes = GetAllRepositoryTypes();
        var repositories = GetRepositories(_serviceProvider, repositoryTypes);

        DestroyAllRepositories(repositories);

        return () => new Result<EmptyResponse, Error>();
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsError(
        ErrorsErrorTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(Error.EntityExists());
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ErrorsThrows(
        ErrorsThrowTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("amessage");
    }

    public async Task<ApiPostResult<string, FormatsTestingOnlyResponse>> FormatsRoundTrip(
        FormatsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return () => new Result<PostResult<FormatsTestingOnlyResponse>, Error>(new FormatsTestingOnlyResponse
        {
            Custom = new CustomDto
            {
                Time = request.Custom?.Time,
                Double = request.Custom?.Double,
                Integer = request.Custom?.Integer,
                String = request.Custom?.String,
                Enum = request.Custom?.Enum
            },
            Double = request.Double,
            Integer = request.Integer,
            String = request.String,
            Time = request.Time,
            Enum = request.Enum
        });
    }

    public async Task<ApiGetResult<string, StringMessageTestingOnlyResponse>> GeneralArrayGet(
        GetWithSimpleArrayTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
                { Message = request.AnArray!.JoinAsOredChoices() });
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> GeneralEmptyBodyPost(
        PostWithEmptyBodyTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse { Message = "amessage" },
                "alocation");
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> GeneralEmptyBodyWithRouteParamsPost(
        PostWithRouteParamsAndEmptyBodyTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse
                    { Message = $"amessage{request.AStringProperty}{request.ANumberProperty}" },
                "alocation");
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> GeneralEnumPost(
        PostWithEnumTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse { Message = $"amessage{request.AnEnum}" },
                "alocation");
    }

    public async Task<ApiEmptyResult> GetInsecure(
        GetInsecureTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return () => new Result<EmptyResponse, Error>();
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> OpenApiGet(
        OpenApiGetTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.RequiredField}" });
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> OpenApiMultiPartForm(
        OpenApiPostMultiPartFormTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse { Message = $"amessage{request.RequiredField}" },
                "alocation");
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> OpenApiPost(
        OpenApiPostTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse { Message = $"amessage{request.RequiredField}" },
                "alocation");
    }

    public async Task<ApiPutPatchResult<string, StringMessageTestingOnlyResponse>> OpenApiPut(
        OpenApiPutTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new StringMessageTestingOnlyResponse { Message = $"amessage{request.RequiredField}" };
    }

    public async Task<ApiEmptyResult> PostInsecure(
        PostInsecureTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return () => new Result<EmptyResponse, Error>();
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> RequestCorrelationGet(
        RequestCorrelationsTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = "amessage" });
    }

    public async Task<ApiDeleteResult> StatusesDelete1(StatusesDeleteTestingOnlyRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<EmptyResponse, Error>(new EmptyResponse());
    }

    public async Task<ApiResult<string, StatusesTestingOnlyResponse>> StatusesDelete2(
        StatusesDeleteWithResponseTestingOnlyRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiGetResult<string, StatusesTestingOnlyResponse>> StatusesGet(
        StatusesGetTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiPostResult<string, StatusesTestingOnlyResponse>> StatusesPost1(
        StatusesPostTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StatusesTestingOnlyResponse>(new StatusesTestingOnlyResponse { Message = "amessage" },
                "alocation");
    }

    public async Task<ApiPostResult<string, StatusesTestingOnlyResponse>> StatusesPost2(
        StatusesPostWithLocationTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StatusesTestingOnlyResponse>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiPutPatchResult<string, StatusesTestingOnlyResponse>> StatusesPutPatch(
        StatusesPutPatchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlyResponse, Error>(new StatusesTestingOnlyResponse { Message = "amessage" });
    }

    public async Task<ApiSearchResult<string, StatusesTestingOnlySearchResponse>> StatusesSearch(
        StatusesSearchTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new Result<StatusesTestingOnlySearchResponse, Error>(new StatusesTestingOnlySearchResponse
                { Messages = new List<string> { "amessage" } });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsUnvalidated(
        ValidationsUnvalidatedTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.Id}" });
    }

    public async Task<ApiResult<string, StringMessageTestingOnlyResponse>> ValidationsValidatedGet(
        ValidationsValidatedGetTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<StringMessageTestingOnlyResponse, Error>(new StringMessageTestingOnlyResponse
            { Message = $"amessage{request.RequiredField}" });
    }

    public async Task<ApiPostResult<string, StringMessageTestingOnlyResponse>> ValidationsValidatedPost(
        ValidationsValidatedPostTestingOnlyRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () =>
            new PostResult<StringMessageTestingOnlyResponse>(
                new StringMessageTestingOnlyResponse { Message = $"amessage{request.RequiredField}" },
                "alocation");
    }

    private static IReadOnlyList<IApplicationRepository> GetRepositories(IServiceProvider services,
        IReadOnlyList<Type> repositoryTypes)
    {
        if (_repositories.NotExists())
        {
            _repositories = repositoryTypes
                .Select(type => Try.Safely(() => services.GetService(type)))
                .OfType<IApplicationRepository>()
                .ToList();
        }

        return _repositories;
    }

    private static IReadOnlyList<Type> GetAllRepositoryTypes()
    {
        if (_allRepositories.NotExists())
        {
            _allRepositories = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes().Where(type =>
                    typeof(IApplicationRepository).IsAssignableFrom(type)
                    && type.IsInterface
                    && type != typeof(IApplicationRepository)))
                .ToList();
        }

        return _allRepositories;
    }

    private static void DestroyAllRepositories(IEnumerable<IApplicationRepository> repositories)
    {
        foreach (var repository in repositories)
        {
            repository.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
#endif