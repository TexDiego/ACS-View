namespace ACS_View.Application.Interfaces;

public interface ICurrentUserContext
{
    int CurrentUserId { get; }
    bool HasCurrentUser { get; }
    void SetCurrentUser(int userId);
    void Clear();
    int RequireCurrentUserId();
}
