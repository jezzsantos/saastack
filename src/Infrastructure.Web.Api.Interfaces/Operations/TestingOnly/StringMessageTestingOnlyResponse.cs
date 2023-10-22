#if TESTINGONLY
namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;

public class StringMessageTestingOnlyResponse : IWebResponse
{
    public string? Message { get; set; }
}
#endif