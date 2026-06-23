using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class ForgotPasswordViewModel(IAuthService authService) : BaseViewModel
    {
        [ObservableProperty] private User? currentUser;
        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string securityQuestion = string.Empty;
        [ObservableProperty] private string answer = string.Empty;
        [ObservableProperty] private string statusMessage = string.Empty;
        [ObservableProperty] private bool isMessageVisible;
        [ObservableProperty] private bool isQuestionVisible;

        public ICommand FetchUserCommand => new Command(async () => await OnFetchUser());
        public ICommand VerifyAnswerCommand => new Command(async () => await OnVerifyAnswer());

        private async Task OnFetchUser()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                IsMessageVisible = true;
                StatusMessage = "Por favor, insira o nome de usuário.";
                return;
            }

            CurrentUser = await authService.GetUserForPasswordRecoveryAsync(Username);

            if (CurrentUser != null)
            {
                SecurityQuestion = CurrentUser.SecurityQuestion;
                IsQuestionVisible = true;
                IsMessageVisible = false;
                StatusMessage = string.Empty;
            }
            else
            {
                IsMessageVisible = true;
                StatusMessage = "Usuário não encontrado.";
            }
        }

        private async Task OnVerifyAnswer()
        {
            if (CurrentUser is null)
            {
                StatusMessage = "Usuário não encontrado.";
                IsMessageVisible = true;
                return;
            }

            try
            {
                const string temporaryPassword = "NovaSenha123";
                await authService.ResetPasswordAsync(CurrentUser.Username, Answer, temporaryPassword);
                StatusMessage = $"Senha redefinida com sucesso! Sua nova senha é: {temporaryPassword}";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }

            IsMessageVisible = true;
        }
    }
}
