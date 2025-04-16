using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public class ForgotPasswordViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private User _currentUser;
        private string _username;
        private string _securityQuestion;
        private string _answer;
        private string _statusMessage;
        private bool _isMessageVisible;
        private bool _isQuestionVisible;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string SecurityQuestion
        {
            get => _securityQuestion;
            set { _securityQuestion = value; OnPropertyChanged(); }
        }

        public string Answer
        {
            get => _answer;
            set { _answer = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsQuestionVisible
        {
            get => _isQuestionVisible;
            set { _isQuestionVisible = value; OnPropertyChanged(); }
        }

        public bool IsMessageVisible
        {
            get => _isMessageVisible;
            set { _isMessageVisible = value; OnPropertyChanged(); }
        }

        public ICommand FetchUserCommand { get; }
        public ICommand VerifyAnswerCommand { get; }

        public ForgotPasswordViewModel() { }

        public ForgotPasswordViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            FetchUserCommand = new Command(OnFetchUser);
            VerifyAnswerCommand = new Command(OnVerifyAnswer);
        }

        private async void OnFetchUser()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                IsMessageVisible = true;
                StatusMessage = "Por favor, insira o nome de usuário.";
                return;
            }

            _currentUser = await _databaseService.GetUserByUsernameAsync(Username);

            if (_currentUser != null)
            {
                SecurityQuestion = _currentUser.SecurityQuestion;
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

        private async void OnVerifyAnswer()
        {
            if (_currentUser != null && _currentUser.SecurityAnswer == Answer)
            {
                // Simplesmente redefine a senha no exemplo
                _currentUser.Password = "NovaSenha123"; // Substitua por uma lógica de redefinição real
                await _databaseService.UpdateUserAsync(_currentUser);
                StatusMessage = "Senha redefinida com sucesso! Sua nova senha é: NovaSenha123";
            }
            else
            {
                StatusMessage = "Resposta incorreta.";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
