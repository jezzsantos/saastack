using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

public class GetCarResponse : IWebResponse
{
    public required Car Car { get; set; }
}