using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Models.Services
{
    public class RegisterValidator : IRegisterValidator
    {
        public bool ValidateBasic(string nome, string susNumber, out string error)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                error = "O campo Nome é obrigatório.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(susNumber))
            {
                error = "O campo Número do SUS é obrigatório.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}