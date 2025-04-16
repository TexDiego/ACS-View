using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModels
{
    public class RegistrationViewModel
    {
        public ObservableCollection<string> SecurityQuestions { get; set; }

        public RegistrationViewModel()
        {
            SecurityQuestions = new ObservableCollection<string>
            {
                "Nome do primeiro animal de estimação",
                "Um prato de comida da sua infância."
            };
        }
    }
}
