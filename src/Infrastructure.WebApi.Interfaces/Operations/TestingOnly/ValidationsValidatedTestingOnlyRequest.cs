#if TESTINGONLY
using JetBrains.Annotations;

namespace Infrastructure.WebApi.Interfaces.Operations.TestingOnly;

[Route("/testingonly/validations/validated/{id}", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ValidationsValidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Field1 { get; set; }

    public string? Field2 { get; set; }

    public string? Id { get; set; }
}
#endif