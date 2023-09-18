#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class GetTestingOnlyValidatedRequest : IWebRequest<GetTestingOnlyResponse>
{
    public string? Id { get; set; }
    public string? Field1 { get; set; }
    public string? Field2 { get; set; }
}
#endif