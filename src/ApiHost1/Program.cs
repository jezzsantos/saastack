using ApiHost1;
using Infrastructure.WebApi.Common;
using JetBrains.Annotations;

var modules = HostedModules.Get();

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterValidators(modules.ApiAssemblies, out var validators);
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblies(modules.ApiAssemblies.ToArray())
        .AddValidatorBehaviors(validators, modules.ApiAssemblies);
});
builder.Services.AddHttpContextAccessor();

modules.RegisterServices(builder.Configuration, builder.Services);

var app = builder.Build();

app.AddExceptionShielding();

//TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
//TODO: need to add swaggerUI (https://www.youtube.com/watch?v=XKN0084p7WQ)

modules.ConfigureHost(app);

app.Run();


namespace ApiHost1
{
    [UsedImplicitly]
    public class Program
    {
    }
}