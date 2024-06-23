using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;

namespace ProjectName.Api.{SubDomainName}s;

public class {SubDomainName}sApi : IWebApiService
{
    private readonly I{SubDomainName}sApplication _{SubDomainNameLower}sApplication;
    private readonly ICallerContextFactory _callerFactory;

    public {SubDomainName}sApi(ICallerContextFactory callerFactory, I{SubDomainName}sApplication {SubDomainNameLower}sApplication)
    {
        _callerFactory = callerFactory;
        _{SubDomainNameLower}sApplication = {SubDomainNameLower}sApplication;
    }
    
    //TODO: Add your service operation methods here
    //Tip: try: postapi and getapi
}