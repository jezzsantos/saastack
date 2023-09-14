using ApiHost1;
using CarsApplication;
using JetBrains.Annotations;
using MinimalApiRegistration = CarsApi.MinimalApiRegistration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(configuration =>
{
    //TODO: need to add API assembly of each module 
    configuration.RegisterServicesFromAssemblies(typeof(ApiHost1.MinimalApiRegistration).Assembly,
        typeof(MinimalApiRegistration).Assembly);
});
builder.Services.AddScoped<ICarsApplication, CarsApplication.CarsApplication>();

//TODO: need register dependencies of each module

var app = builder.Build();

//TODO: need to add validation
//TODO: need to add swaggerUI


//TODO: need to call the registration function the minimal API endpoints of each module
app.RegisterRoutes();
MinimalApiRegistration.RegisterRoutes(app);

app.Run();


[UsedImplicitly]
public partial class Program
{
}