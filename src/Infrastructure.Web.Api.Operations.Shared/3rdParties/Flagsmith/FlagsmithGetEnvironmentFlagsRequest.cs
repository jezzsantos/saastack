using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/flags/", OperationMethod.Get)]
[UsedImplicitly]
public class FlagsmithGetEnvironmentFlagsRequest : IWebRequest<FlagsmithGetEnvironmentFlagsResponse>;