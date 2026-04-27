namespace CHAP2.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string UserName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAdmin { get; }
}
