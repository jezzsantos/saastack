using Application.Interfaces;
using IdentityApplication;
using Infrastructure.Web.Api.Interfaces;

namespace IdentityInfrastructure.Api.ApiKeys;

public class ApiKeysApi : IWebApiService
{
    private readonly IApiKeysApplication _apiKeysApplication;
    private readonly ICallerContext _context;

    public ApiKeysApi(ICallerContext context, IApiKeysApplication apiKeysApplication)
    {
        _context = context;
        _apiKeysApplication = apiKeysApplication;
    }
}