namespace OtoIsmail.Abstract;
public interface ITokenService
{
    Task<string> GetTokenAsync();
}