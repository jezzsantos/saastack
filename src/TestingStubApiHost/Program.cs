using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using JetBrains.Annotations;
using TestingStubApiHost;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, WebHostOptions.TestingStubsHost);
app.Run();

namespace TestingStubApiHost
{
    [UsedImplicitly]
    public class Program
    {
    }
}