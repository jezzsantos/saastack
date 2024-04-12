using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

public class GetImageResponse : IWebResponse
{
    public Image? Image { get; set; }
}