using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class FamiliesViewModel : BaseViewModel
    {
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        private readonly IVisitsService _visitsService = App.ServiceProvider.GetRequiredService<IVisitsService>();

        [ObservableProperty] private ObservableCollection<Familia> families = [];
        [ObservableProperty] private string house = "";

        public IRelayCommand AddFamilyCommand => new RelayCommand(AddFamily);
        public IRelayCommand<int> DeleteFamilyCommand => new RelayCommand<int>(DeleteFamily);
        public IRelayCommand<int> EditFamilyCommand => new RelayCommand<int>(EditFamily);
        public IRelayCommand<int> VisitFamilyCommand => new RelayCommand<int>(VisitFamily);
        public ICommand PersonInfo => new Command<string>(async susNumber => await PersonData(susNumber));
        public ICommand GoBack => new Command(async () => await Shell.Current.GoToAsync(".."));


        private readonly int _idHouse = 0;

        public FamiliesViewModel(int idHouse)
        {
            _idHouse = idHouse;
            LoadFamilies();
        }

        public async void LoadFamilies()
        {
            try
            {
                var house = await _houseService.GetHouseByIdAsync(_idHouse);

                if (house == null)
                {
                    await Shell.Current.DisplayAlert("Erro", "Casa não encontrada.", "OK");
                    return;
                }

                string rua = house.Rua ?? "Famílias";
                string numero = house.NumeroCasa ?? "";
                string complemento = house.Complemento ?? "";

                House = complemento == "" ? $"{rua}\n Nº {numero}" : $"{rua}\n Nº {numero} - {complemento}";

                Console.WriteLine(House);

                var familiesFromDb = await _healthRecordService.GetRecordsByHouseIdAsync(_idHouse);
                var familiesGrouped = familiesFromDb
                    .Where(p => p.FamilyId > 0)
                    .GroupBy(p => p.FamilyId)
                    .Select(g => new Familia
                    {
                        IdFamily = g.Key,
                        PessoasFamilia = new ObservableCollection<Patient>([.. g])
                    }).ToList();

                Families.Clear();

                foreach (var family in familiesGrouped)
                {
                    Families.Add(family);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        private async void AddFamily()
        {
            try
            {
                var newFamily = new Familia
                {
                    PessoasFamilia = [] // Cria uma nova lista de pessoas
                };

                Families.Add(newFamily); // Adiciona a nova família à coleção
                await Shell.Current.Navigation.PushAsync(new AddFamilyPage(_idHouse, false));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível adicionar a nova família.\n\n{ex.Message}", "OK");
            }
        }

        private async void DeleteFamily(int idFamily)
        {
            try
            {
                var confirm = await Shell.Current.ShowPopupAsync(new DisplayPopUp(
                    "Confirmar Exclusão",
                    $"Tem certeza de que deseja excluir a família?",
                    true ,"Excluir",
                    true, "Cancelar"));

                if ((bool)confirm) return;

                var pessoasDaFamilia = await _healthRecordService.GetRecordsByFamilyAndHouseAsync(idFamily, _idHouse);

                foreach (var person in pessoasDaFamilia)
                {
                    person.HouseId = 0;
                    person.FamilyId = 0;

                    await _healthRecordService.UpdateRecordAsync(person);
                }

                OnPropertyChanged(nameof(Families));
                LoadFamilies();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Erro",
                    $"Não foi possível excluir a família.\n\n{ex.Message}",
                    "OK");
            }
        }

        public async void EditFamily(int idFamily)
        {
            try
            {
                var familyToEdit = Families.FirstOrDefault(f => f.IdFamily == idFamily);

                if (familyToEdit == null)
                {
                    await Shell.Current.DisplayAlert("Aviso", "Família não encontrada.", "OK");
                    return;
                }

                await Shell.Current.Navigation.PushAsync(new AddFamilyPage(_idHouse, true, idFamily));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível editar a família.\n\n{ex.Message}", "OK");
            }
        }

        public async void VisitFamily(int idFamily)
        {
            try
            {
                var familyToVisit = Families.FirstOrDefault(f => f.IdFamily == idFamily);

                if (familyToVisit == null)
                {
                    await Shell.Current.DisplayAlert("Aviso", "Família não encontrada.", "OK");
                    return;
                }

                var visit = await Shell.Current.ShowPopupAsync(new VisitPage(_idHouse, idFamily));

                if (visit is Visits visits)
                {
                    await _visitsService.RegisterVisitAsync(visits);
                    await Shell.Current.DisplayAlert("Sucesso", "Visita realizada", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Aviso", "Visita não registrada.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Não foi possível visitar a família.\n\n{ex.Message}", "OK");
            }
        }

        private async Task PersonData(string susNumber)
        {
            try
            {
                var record = await _healthRecordService.GetRecordBySusAsync(susNumber);

                if (record != null)
                {
                    var popup = new PersonsInfo(record);
                    await Shell.Current.ShowPopupAsync(popup);
                }
            }
            catch
            {
                await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", "Erro ao carregar os dados da pessoa.", true, "Fechar", false, ""));
            }
        }
    }
}
