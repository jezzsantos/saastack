namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines options for determining the response code
/// </summary>
public class ResponseCodeOptions
{
    public ResponseCodeOptions(bool hasContent, bool hasLocation)
    {
        HasContent = hasContent;
        HasLocation = hasLocation;
    }

    public bool HasContent { get; }

    public bool HasLocation { get; }
}