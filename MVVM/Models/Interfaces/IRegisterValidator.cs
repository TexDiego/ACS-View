namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IRegisterValidator
    {
        bool ValidateBasic(string nome, string susNumber, out string error);
    }
}