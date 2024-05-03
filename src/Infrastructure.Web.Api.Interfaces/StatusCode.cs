using System.Net;
using Common;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     An HTTP status code
/// </summary>
public record StatusCode
{
    public static StatusCode Accepted => new(HttpStatusCode.Accepted);

    public static StatusCode BadRequest => new(HttpStatusCode.BadRequest);

    public static StatusCode Conflict => new(HttpStatusCode.Conflict);

    public static StatusCode Created => new(HttpStatusCode.Created);

    public static StatusCode Forbidden => new(HttpStatusCode.Forbidden);

    public static StatusCode InternalServerError => new(HttpStatusCode.InternalServerError);

    public static StatusCode MethodNotAllowed => new(HttpStatusCode.MethodNotAllowed);

    public static StatusCode NoContent => new(HttpStatusCode.NoContent);

    public static StatusCode NotFound => new(HttpStatusCode.NotFound);

    public static StatusCode Ok => new(HttpStatusCode.OK);

    public static StatusCode PaymentRequired => new(HttpStatusCode.PaymentRequired);

    public static StatusCode Unauthorized => new(HttpStatusCode.Unauthorized);

    public StatusCode(HttpStatusCode code)
    {
        Code = code;
        Numeric = (int)code;

        Title = Resources.ResourceManager.GetString($"HttpConstants_StatusCodes_Title_{code.ToString()}")
                ?? code.ToString();
        Reason = Resources.ResourceManager.GetString($"HttpConstants_StatusCodes_Reason_{code.ToString()}")
                 ?? code.ToString();
        HttpErrorCode = Enum.IsDefined(typeof(HttpErrorCode), (int)code)
            ? (HttpErrorCode)(int)code
            : null;
        ErrorCodes = HttpConstants.StatusCodes.SupportedErrorCodesMap.GetValueOrDefault(code);
    }

    public HttpStatusCode Code { get; }

    public IReadOnlyList<ErrorCode>? ErrorCodes { get; }

    public HttpErrorCode? HttpErrorCode { get; }

    public int Numeric { get; }

    public string Reason { get; }

    public string Title { get; }
}