using CarsApi;
using CarsApplication;
using Infrastructure.WebApi.Common;
using JetBrains.Annotations;

//TODO: Add the modules of each API here
var modules = new SubDomainModules();
modules.Register(new Module());
modules.Register(new ApiHost1.Module());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblies(modules.HandlerAssemblies.ToArray());
});
builder.Services.AddScoped<ICarsApplication, CarsApplication.CarsApplication>();

modules.RegisterServices(builder.Configuration, builder.Services);

var app = builder.Build();

//TODO: need to add validation (https://www.youtube.com/watch?v=XKN0084p7WQ)
//TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
//TODO: need to add swaggerUI (https://www.youtube.com/watch?v=XKN0084p7WQ)

modules.ConfigureHost(app);

app.Run();


[UsedImplicitly]
public partial class Program
{
}