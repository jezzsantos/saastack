using Application.Interfaces;

#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class StatusesTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}

public class StatusesTestingOnlySearchResponse : IWebSearchResponse
{
    public List<string>? Messages { get; set; }

    public SearchResultMetadata? Metadata { get; set; }
}
#endif