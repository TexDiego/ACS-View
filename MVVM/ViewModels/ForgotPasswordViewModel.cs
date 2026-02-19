using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class ForgotPasswordViewModel : BaseViewModel
    {
        private readonly IDatabaseService databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        
        [ObservableProperty] private User currentUser;
        [ObservableProperty] private string username;
        [ObservableProperty] private string securityQuestion;
        [ObservableProperty] private string answer;
        [ObservableProperty] private string statusMessage;
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

            CurrentUser = await databaseService.GetUserByUsernameAsync(Username);

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
            if (CurrentUser != null && CurrentUser.SecurityAnswer == Answer)
            {
                // Simplesmente redefine a senha no exemplo
                CurrentUser.Password = "NovaSenha123"; // Substitua por uma lógica de redefinição real
                await databaseService.UpdateUserAsync(CurrentUser);
                StatusMessage = "Senha redefinida com sucesso! Sua nova senha é: NovaSenha123";
            }
            else
            {
                StatusMessage = "Resposta incorreta.";
            }
        }
    }
}