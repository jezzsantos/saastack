using ApiHost1;
using Infrastructure.WebApi.Common;
using JetBrains.Annotations;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, WebHostOptions.BackEndApiHost);
app.Run();

namespace ApiHost1
{
    [UsedImplicitly]
    public class Program
    {
    }
}