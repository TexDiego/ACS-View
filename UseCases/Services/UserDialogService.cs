using ACS_View.Domain.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Maui.Views;

namespace ACS_View.UseCases.Services
{
    public class UserDialogService : IUserDialogService
    {
        public Task ShowError(string message) =>
            Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", message, true, "Fechar", false, ""));

        public Task ShowSuccess(string message) =>
            Shell.Current.ShowPopupAsync(new DisplayPopUp("Sucesso", message, false, "", true, "OK"));
    }
}