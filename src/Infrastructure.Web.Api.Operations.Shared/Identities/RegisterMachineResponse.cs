using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegisterMachineResponse : IWebResponse
{
    public required MachineCredential Machine { get; set; }
}