namespace Domain.Services.Shared.DomainServices;

public interface ITokensService
{
    string CreateTokenForJwtRefresh();

    string CreateTokenForPasswordReset();

    string CreateTokenForVerification();
}