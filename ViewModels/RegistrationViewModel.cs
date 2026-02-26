using System.Collections.ObjectModel;

namespace ACS_View.ViewModels
{
    public class RegistrationViewModel
    {
        public ObservableCollection<string> SecurityQuestions { get; set; }

        public RegistrationViewModel()
        {
            SecurityQuestions =
            [
                "Nome do primeiro animal de estimação",
                "Um prato de comida da sua infância."
            ];
        }
    }
}
