namespace ACS_View.Domain.Interfaces
{
    public interface IUserDialogService
    {
        Task ShowSuccess(string message);
        Task ShowError(string message);
    }
}
