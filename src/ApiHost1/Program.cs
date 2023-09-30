using ApiHost1;
using Infrastructure.Common.Recording;
using Infrastructure.WebApi.Common;
using JetBrains.Annotations;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, RecorderOptions.BackEndApiHost);
app.Run();

namespace ApiHost1
{
    [UsedImplicitly]
    public class Program
    {
    }
}