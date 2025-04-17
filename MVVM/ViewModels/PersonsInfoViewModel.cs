using ACS_View.MVVM.Models;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModels
{
    public class PersonsInfoViewModel
    {
        public ObservableCollection<HealthIcon> Icons { get; set; } = new();
        public HealthRecord PersonInfo { get; set; }

        public PersonsInfoViewModel() { }
        public PersonsInfoViewModel(HealthRecord record)
        {
            PersonInfo = record;
            SetHealthIcons(record);
        }

        private void SetHealthIcons(HealthRecord record)
        {
            try
            {
                if (record.IsPregnant)
                    Icons.Add(new HealthIcon { IconSource = "pregnancy.png", Description = "Gestante" });

                if (record.HasDiabetes)
                    Icons.Add(new HealthIcon { IconSource = "diabetes.png", Description = "Diabetes" });

                if (record.HasHypertension)
                    Icons.Add(new HealthIcon { IconSource = "hypertension.png", Description = "Hipertensão" });

                if (record.HasTuberculosis)
                    Icons.Add(new HealthIcon { IconSource = "tuberculosis.png", Description = "Tuberculose" });

                if (record.HasLeprosy)
                    Icons.Add(new HealthIcon { IconSource = "leprosy.png", Description = "Hanseníase" });

                if (record.IsBedridden)
                    Icons.Add(new HealthIcon { IconSource = "disability.png", Description = "Acamado" });

                if (record.HasCancer)
                    Icons.Add(new HealthIcon { IconSource = "cancer.png", Description = "Câncer" });

                if (record.HasMentalIllness)
                    Icons.Add(new HealthIcon { IconSource = "mentalhealth.png", Description = "Doença Mental" });

                if (record.IsSmoker)
                    Icons.Add(new HealthIcon { IconSource = "cigarrete.png", Description = "Tabagista" });

                if (record.IsOld)
                    Icons.Add(new HealthIcon { IconSource = "oldman.png", Description = "Idoso" });
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar ícones de saúde: {ex.Message}", "OK");
            }
        }
    }
}
