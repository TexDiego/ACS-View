namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IUserDialogService
    {
        Task ShowSuccess(string message);
        Task ShowError(string message);
    }
}
