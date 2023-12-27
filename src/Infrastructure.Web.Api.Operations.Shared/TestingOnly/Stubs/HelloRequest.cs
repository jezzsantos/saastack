using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly.Stubs;

[Route("/hello", ServiceOperation.Get, isTestingOnly: true)]
[UsedImplicitly]
public class HelloRequest : IWebRequest<HelloResponse>
{
}