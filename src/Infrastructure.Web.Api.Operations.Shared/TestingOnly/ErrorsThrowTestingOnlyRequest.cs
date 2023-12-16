using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

#if TESTINGONLY
namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

[Route("/testingonly/errors/throws", ServiceOperation.Get, true)]
[UsedImplicitly]
public class ErrorsThrowTestingOnlyRequest : IWebRequest<StringMessageTestingOnlyResponse>
{
}
#endif