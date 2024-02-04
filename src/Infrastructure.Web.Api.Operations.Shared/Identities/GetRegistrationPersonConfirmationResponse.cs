#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetRegistrationPersonConfirmationResponse : IWebResponse
{
    public string? Token { get; set; }
}
#endif