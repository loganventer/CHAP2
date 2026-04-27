namespace CHAP2.WebPortal.Auth;

public interface ITokenStore
{
    StoredTokens? Read();
    Task WriteAsync(StoredTokens tokens, CancellationToken cancellationToken = default);
}
