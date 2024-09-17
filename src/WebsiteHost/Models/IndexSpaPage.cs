namespace WebsiteHost.Models;

public class IndexSpaPage
{
    public required string CSRFFieldName { get; set; }

    public required string CSRFHeaderToken { get; set; }

    public bool IsTestingOnly { get; set; }
}