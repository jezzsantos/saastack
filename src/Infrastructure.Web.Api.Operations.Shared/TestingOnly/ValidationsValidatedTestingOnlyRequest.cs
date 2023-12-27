using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/validations/validated/{id}", ServiceOperation.Get, isTestingOnly: true)]
[UsedImplicitly]
public class ValidationsValidatedTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
    public string? Field1 { get; set; }

    public string? Field2 { get; set; }

    public string? Id { get; set; }
}
#endif