using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Interfaces.Operations.TestingOnly.Stubs;

[Route("/hello", ServiceOperation.Get, true)]
[UsedImplicitly]
public class HelloRequest : IWebRequest<HelloResponse>
{
}