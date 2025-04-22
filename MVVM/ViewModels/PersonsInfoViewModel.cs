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
            Console.WriteLine(record.HasNothing);
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

                if (record.IsHomebound)
                    Icons.Add(new HealthIcon { IconSource = "disability.png", Description = "Acamado" });

                if (record.HasCancer)
                    Icons.Add(new HealthIcon { IconSource = "cancer.png", Description = "Câncer" });

                if (record.HasMentalIllness)
                    Icons.Add(new HealthIcon { IconSource = "mentalhealth.png", Description = "Psiquiátrico" });

                if (record.IsSmoker)
                    Icons.Add(new HealthIcon { IconSource = "cigarrete.png", Description = "Tabagista" });

                if (record.IsAlcoholic)
                    Icons.Add(new HealthIcon { IconSource = "drunk.png", Description = "Álcool" });

                if (record.IsOld)
                    Icons.Add(new HealthIcon { IconSource = "oldman.png", Description = "Idoso" });

                if (record.IsNeurodivergent)
                    Icons.Add(new HealthIcon { IconSource = "neurodivergence.png", Description = "Neurodivergente" });

                if (record.BolsaFamilia)
                    Icons.Add(new HealthIcon { IconSource = "bolsafamilia.png", Description = "Bolsa Família" });

                if (record.HasHeartDesease)
                    Icons.Add(new HealthIcon { IconSource = "heart.png", Description = "Doença Cardíaca" });

                if (record.HasKidneyDesease)
                    Icons.Add(new HealthIcon { IconSource = "kidney.png", Description = "Doença Renal" });

                if (record.HasLiverDesease)
                    Icons.Add(new HealthIcon { IconSource = "liver.png", Description = "Doença Hepática" });

                if (record.HasLungsDesease)
                    Icons.Add(new HealthIcon { IconSource = "lungs.png", Description = "Doença Pulmonar" });

                if (record.IsDrugAddicted)
                    Icons.Add(new HealthIcon { IconSource = "pills.png", Description = "Dependência Química" });

                if (record.HasHIV)
                    Icons.Add(new HealthIcon { IconSource = "virus.png", Description = "Imunodeficiencia" });
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao carregar ícones de saúde: {ex.Message}", "OK");
            }
        }
    }
}
