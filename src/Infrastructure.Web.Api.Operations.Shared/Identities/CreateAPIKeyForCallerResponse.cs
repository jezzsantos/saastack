using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class CreateAPIKeyForCallerResponse : IWebResponse
{
    public required string ApiKey { get; set; }
}