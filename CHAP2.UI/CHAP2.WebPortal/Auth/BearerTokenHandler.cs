using System.Net.Http.Headers;

namespace CHAP2.WebPortal.Auth;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;

    public BearerTokenHandler(ITokenStore tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stored = _tokens.Read();
        if (stored is not null && !string.IsNullOrEmpty(stored.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stored.AccessToken);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
