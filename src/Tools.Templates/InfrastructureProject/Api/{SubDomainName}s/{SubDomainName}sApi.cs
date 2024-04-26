namespace ProjectName.Api.{SubDomainName}s;

public class {SubDomainName}sApi : IwebApiService
{
    private readonly I{SubDomainName}sApplication _{SubDomainNameLower}sApplication;
    private readonly ICallerContextFactory _callerFactory;

    public CarsApi(ICallerContextFactory callerFactory, I{SubDomainName}sApplication {SubDomainNameLower}sApplication)
    {
        _callerFactory = callerFactory;
        _{SubDomainNameLower}sApplication = {SubDomainNameLower}sApplication;
    }
    
    //TODO: Add your service operation methods here
}