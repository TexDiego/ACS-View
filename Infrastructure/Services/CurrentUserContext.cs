using ACS_View.Application.Interfaces;

namespace ACS_View.Infrastructure.Services;

internal sealed class CurrentUserContext : ICurrentUserContext
{
    public int CurrentUserId { get; private set; }
    public bool HasCurrentUser => CurrentUserId > 0;

    public void SetCurrentUser(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "Usuario invalido.");
        }

        CurrentUserId = userId;
    }

    public void Clear()
    {
        CurrentUserId = 0;
    }

    public int RequireCurrentUserId()
    {
        if (!HasCurrentUser)
        {
            throw new InvalidOperationException("Nao ha usuario autenticado.");
        }

        return CurrentUserId;
    }
}
