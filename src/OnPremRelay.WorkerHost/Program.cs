using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OnPremRelay.WorkerHost.Extensions;
using Prometheus;

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile("appsettings.OnPremises.json", true, false)
            .AddEnvironmentVariables();
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseKestrel()
            .ConfigureServices(services => { services.AddRouting(); })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapMetrics();
                    endpoints.MapGet("/health", async context =>
                    {
                        // Lógica de verificación de salud personalizada
                        // var isHealthy = await CheckHealthAsync();
                        var isHealthy = true;
                        context.Response.StatusCode = isHealthy
                            ? StatusCodes.Status200OK
                            : StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsync(isHealthy
                            ? "Healthy"
                            : "Unhealthy");
                    });

                    endpoints.MapPost("{*path}", async (HttpContext context, string path) =>
                    {
                        var receiptId = $"receipt_{Guid.NewGuid():N}";
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                        if (path.Contains("event_notifications"))
                        {
                        }

                        // Endpoint para Mailgun
                        if (path.Contains("mailgun/"))
                        {
                            try
                            {
                                // var userPilotApi = context.RequestServices.GetRequiredService<MailgunApi>();
                                // var request = JsonSerializer.Deserialize<MailgunSendMessageRequest>(requestDetails.Body);
                                // if (request == null)
                                // {
                                //     return Results.BadRequest("Invalid request body");
                                // }
                                //
                                // var result = await userPilotApi.SendMessage(request, context.RequestAborted);
                                // return Results.Ok(result);
                                return Results.Ok(new
                                    {
                                        Id = receiptId,
                                        Message = "Queued. Thank you.",
                                        Path = path
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error en TrackEvent");
                                return Results.Problem("Error processing request");
                            }
                        }

                        if (path.Contains("usages/deliver"))
                        {
                            return Results.Ok(new
                                {
                                    Id = receiptId,
                                    Message = "Queued. Thank you.",
                                    Path = path
                                }
                            );
                        }

                        return Results.Ok(new
                            {
                                Id = receiptId,
                                Message = "Queued. Thank you.",
                                Path = "UNKNOW - " + path
                            }
                        );
                    });

                    endpoints.MapPost("/userpilot/{*path}", async (HttpContext context, string path) =>
                    {
                        var receiptId = $"receipt_{Guid.NewGuid():N}";
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                        var requestDetails = new
                        {
                            Body = await new StreamReader(context.Request.Body).ReadToEndAsync()
                        };

                        logger.LogInformation("Petición POST recibida: {@RequestDetails}", path);

                        if (path.Contains("identify"))
                        {
                            try
                            {
                                // var userPilotApi = context.RequestServices.GetRequiredService<UserPilotApi>();
                                // var request = JsonSerializer.Deserialize<UserPilotIdentifyUserRequest>(requestDetails.Body);
                                // if (request == null)
                                // {
                                //     return Results.BadRequest("Invalid request body");
                                // }
                                //
                                // await userPilotApi.IdentifyUser(request, context.RequestAborted);
                                return Results.Ok(new
                                    {
                                        Id = receiptId,
                                        Message = "Queued. Thank you."
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error en IdentifyUser");
                                return Results.Problem("Error processing request");
                            }
                        }

                        if (path.Contains("track"))
                        {
                            try
                            {
                                // var userPilotApi = context.RequestServices.GetRequiredService<UserPilotApi>();
                                // var request = JsonSerializer.Deserialize<UserPilotTrackEventRequest>(requestDetails.Body);
                                // if (request == null)
                                // {
                                //     return Results.BadRequest("Invalid request body");
                                // }
                                //
                                // await userPilotApi.TrackEvent(request, context.RequestAborted);
                                return Results.Ok(new
                                    {
                                        Id = receiptId,
                                        Message = "Queued. Thank you."
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error en TrackEvent");
                                return Results.Problem("Error processing request");
                            }
                        }

                        return Results.Ok(new
                            {
                                Id = receiptId,
                                Message = "Queued. Thank you."
                            }
                        );
                    });
                });
            });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "hh:mm:ss ";
                options.SingleLine = true;
                options.IncludeScopes = false;
            });
            builder.AddConsole();
            builder.AddDebug();
            builder.AddEventSourceLogger();
        });
        services.AddMessageProcessing(context.Configuration);
    })
    .Build();

await host.RunAsync();

namespace AzureFunctions.Api.WorkerHost
{
    [UsedImplicitly]
    public class Program;
}