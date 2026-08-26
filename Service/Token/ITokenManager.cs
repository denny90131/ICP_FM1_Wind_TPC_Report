public interface ITokenManager
{
    string? GetToken();
    void SetToken(string newToken);
}