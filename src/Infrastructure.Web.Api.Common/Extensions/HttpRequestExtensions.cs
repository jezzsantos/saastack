using System.Text;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpRequestExtensions
{
    /// <summary>
    ///     Returns the uploaded file from the specified <see cref="request" />,
    ///     given a specified <see cref="maxSizeInBytes" /> and <see cref="allowableContentTypes" />
    /// </summary>
    public static Result<FileUpload, Error> GetUploadedFile(this HttpRequest request,
        IFileUploadService fileUploadService, long maxSizeInBytes, IReadOnlyList<string> allowableContentTypes)
    {
        var uploads = request.Form.Files
            .Select(file => new FileUpload
            {
                Content = file.OpenReadStream(),
                ContentType = FileUploadContentType.FromContentType(file.ContentType),
                Filename = file.FileName,
                Size = file.Length
            }).ToList();

        return fileUploadService.GetUploadedFile(uploads, maxSizeInBytes, allowableContentTypes);
    }

    /// <summary>
    ///     Whether the specified HMAC signature represents the signature of the contents of the inbound request,
    ///     serialized by the method
    ///     <see cref="RequestExtensions.SerializeToJson(Infrastructure.Web.Api.Interfaces.IWebRequest?)" />
    /// </summary>
    public static async Task<bool> VerifyHMACSignatureAsync(this HttpRequest request, string signature, string secret,
        CancellationToken cancellationToken)
    {
        if (request.Body.Position != 0)
        {
            request.RewindBody();
        }

        var body = await request.Body.ReadFullyAsync(cancellationToken);
        request.RewindBody(); // HACK: need to do this for later middleware

        if (body.Length == 0)
        {
            body = Encoding.UTF8.GetBytes(RequestExtensions
                .EmptyRequestJson); //HACK: we assume that an empty JSON request was signed
        }

        var signer = new HMACSigner(body, secret);
        var verifier = new HMACVerifier(signer);

        return verifier.Verify(signature);
    }
}