#if TESTINGONLY
namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

public class GetTestingOnlyRequest : IWebRequest<GetTestingOnlyResponse>
{
    public string? Id { get; set; }
}
#endif