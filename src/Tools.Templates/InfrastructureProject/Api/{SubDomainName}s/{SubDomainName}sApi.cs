namespace ProjectName.Api.{SubDomainName}s;

public class {SubDomainName}sApi : IwebApiService
{
    private readonly I{SubDomainName}sApplication _{SubDomainNameLower}sApplication;
    private readonly ICallerContext _context;

    public CarsApi(ICallerContext context, I{SubDomainName}sApplication {SubDomainNameLower}sApplication)
    {
        _context = context;
        _{SubDomainNameLower}sApplication = {SubDomainNameLower}sApplication;
    }
    
    //TODO: Add your service operation methods here
}