using ProjectName;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using JetBrains.Annotations;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, WebHostOptions.BackEndApiHost);
app.Run();

namespace ProjectName
{
    [UsedImplicitly]
    public class Program
    {
    }
}