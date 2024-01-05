using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class RegisterMachineResponse : IWebResponse
{
    public MachineCredential? Machine { get; set; }
}