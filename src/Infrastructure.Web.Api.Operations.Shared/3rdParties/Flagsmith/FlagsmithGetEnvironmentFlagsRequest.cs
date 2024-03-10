using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/flags/", ServiceOperation.Get)]
[UsedImplicitly]
public class FlagsmithGetEnvironmentFlagsRequest : IWebRequest<FlagsmithGetEnvironmentFlagsResponse>;