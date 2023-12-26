using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Health;

namespace WebsiteHost.Api.Health;

public sealed class HealthApi : IWebApiService
{
    public async Task<ApiResult<string, HealthCheckResponse>> Check(HealthCheckRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return () => new Result<HealthCheckResponse, Error>(new HealthCheckResponse
        {
            Name = nameof(WebsiteHost),
            Status = "OK"
        });
    }
}