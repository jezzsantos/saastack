#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class StatusesTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}
#endif