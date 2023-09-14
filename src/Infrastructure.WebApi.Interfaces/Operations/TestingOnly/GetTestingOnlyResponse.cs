#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class GetTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}
#endif