using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Signings;

public class CreateDraftSigningRequestResponse : IWebResponse
{
    public SigningRequest? SigningRequest { get; set; }
}