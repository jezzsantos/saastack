using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Fetches all flags for an environment
/// </summary>
[Route("/flags/", OperationMethod.Get)]
[UsedImplicitly]
public class
    FlagsmithGetEnvironmentFlagsRequest : WebRequest<FlagsmithGetEnvironmentFlagsRequest,
    FlagsmithGetEnvironmentFlagsResponse>;