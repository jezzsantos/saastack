using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using NuDoq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds details to the operation from XML documentation.
///     Note: This filter relies on the fact that XML documentation for each assembly
///     (containing <see cref="IWebRequest" />) are generated
///     using the <GenerateDocumentationFile>true</GenerateDocumentationFile> MSBUILD property
/// </summary>
[UsedImplicitly]
public sealed class XmlDocumentationOperationFilter : IOperationFilter
{
    public const string ErrorCodeAttributeName = "code";
    public const string ResponseElementName = "response";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var type = context.GetRequestType();
        if (!type.HasValue)
        {
            return;
        }

        var requestType = type.Value;
        var reader = DocReader.Read(requestType.Assembly);
        var memberId = reader.IdMap.FindId(requestType);
        var requestClass = reader.Elements
            .OfType<Class>()
            .FirstOrDefault(cls => cls.Id == memberId);

        var summary = GetClassSummary(requestClass);
        if (summary.HasValue())
        {
            operation.Summary = summary;
        }

        var responses = GetClassCustomResponses(requestClass);
        if (responses.HasAny())
        {
            operation.Responses = responses;
        }
    }

    private static OpenApiResponses GetClassCustomResponses(Class? requestClass)
    {
        if (requestClass.NotExists())
        {
            return new OpenApiResponses();
        }

        var docResponses = requestClass.Elements
            .OfType<UnknownElement>()
            .Where(ele =>
                ele.Xml.Name.LocalName == ResponseElementName
                && ele.Xml.FirstAttribute.Exists()
                && ele.Xml.FirstAttribute?.Name.LocalName == ErrorCodeAttributeName)
            .ToList();
        if (docResponses.HasNone())
        {
            return new OpenApiResponses();
        }

        var responses = new OpenApiResponses();
        foreach (var docResponse in docResponses)
        {
            var code = docResponse.Xml.FirstAttribute!.Value;
            var description = docResponse.Xml.Value;

            responses.Add(code, new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>()
            });
        }

        return responses;
    }

    private static string? GetClassSummary(Class? requestClass)
    {
        if (requestClass.NotExists())
        {
            return null;
        }

        var docSummary = requestClass.Elements
            .OfType<Summary>()
            .FirstOrDefault();
        if (docSummary.NotExists())
        {
            return null;
        }

        var summary = docSummary.ToText();
        return summary.HasNoValue()
            ? null
            : summary;
    }
}