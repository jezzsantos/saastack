using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Health;

namespace ApiHost1.Api.Health;

public sealed class HealthApi : IWebApiService
{
    public async Task<ApiResult<string, HealthCheckResponse>> Check(HealthCheckRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<HealthCheckResponse, Error>(new HealthCheckResponse
        {
            Name = nameof(ApiHost1),
            Status = "OK"
        });
    }
}