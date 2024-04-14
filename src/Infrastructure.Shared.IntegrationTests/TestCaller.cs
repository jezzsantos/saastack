using Application.Interfaces;
using Common;

namespace Infrastructure.Shared.IntegrationTests;

public class TestCaller : ICallerContext
{
    public Optional<ICallerContext.CallerAuthorization> Authorization =>
        Optional<ICallerContext.CallerAuthorization>.None;

    public string CallerId => "acallerid";

    public string CallId => "acallid";

    public ICallerContext.CallerFeatures Features => new();

    public bool IsAuthenticated => false;

    public bool IsServiceAccount => false;

    public ICallerContext.CallerRoles Roles => new();

    public string? TenantId => null;
}