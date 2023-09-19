#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class StringMessageTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}
#endif