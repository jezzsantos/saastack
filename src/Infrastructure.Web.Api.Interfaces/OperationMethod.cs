namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the methods of API service operations
/// </summary>
public enum OperationMethod
{
    Get,
    Search, //A GET method, that returns search results
    Post,
    PutPatch, //Either PUT or PATCH
    Delete
}