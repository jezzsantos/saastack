using Application.Interfaces;
using Infrastructure.Web.Api.Interfaces;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

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