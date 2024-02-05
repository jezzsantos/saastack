using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a registration of middleware
/// </summary>
public class MiddlewareRegistration
{
    public MiddlewareRegistration(int priority, Action<WebApplication> action,
        [StructuredMessageTemplate] string message,
        params object[] args)
    {
        Priority = priority;
        Message = message;
        MessageArgs = args;
        Action = action;
    }

    public Action<WebApplication> Action { get; }

    public string Message { get; }

    public object[] MessageArgs { get; }

    public int Priority { get; set; }

    public void Register(WebApplication app)
    {
        app.Logger.LogInformation(Message, MessageArgs);
        Action(app);
    }
}