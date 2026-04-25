namespace CHAP2.Shared.Configuration;

/// <summary>
/// Thrown by the WebPortal's ChorusApiService when a call to the
/// chorus API fails at the transport layer (timeout, refused
/// connection, non-2xx status). Distinct from "API answered with
/// zero results" so the controller can surface a 503 to the browser
/// and let the UI's probe / overlay flow react, instead of pretending
/// a successful empty response.
/// </summary>
public sealed class ApiUnavailableException : Exception
{
    public ApiUnavailableException(string message) : base(message) { }
    public ApiUnavailableException(string message, Exception inner) : base(message, inner) { }
}
