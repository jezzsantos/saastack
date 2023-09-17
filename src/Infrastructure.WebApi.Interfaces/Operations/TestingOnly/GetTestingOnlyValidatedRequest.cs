#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class GetTestingOnlyValidatedRequest : IWebRequest<GetTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif