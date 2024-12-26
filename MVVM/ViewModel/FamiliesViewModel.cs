using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModels
{
    public partial class FamiliesViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly DatabaseService _databaseService;

        public ObservableCollection<Familia> Families { get; } = new ObservableCollection<Familia>();
        public IRelayCommand AddFamilyCommand { get; }
        public IRelayCommand<int> DeleteFamilyCommand { get; }
        public IRelayCommand<int> EditFamilyCommand { get; }

        public int _idHouse;

        public FamiliesViewModel() { }
        public FamiliesViewModel(int idHouse)
        {
            _idHouse = idHouse;
            _databaseService = new DatabaseService();
            _healthRecordService = new HealthRecordService(_databaseService);

            AddFamilyCommand = new RelayCommand(AddFamily);
            DeleteFamilyCommand = new RelayCommand<int>(DeleteFamily);
            EditFamilyCommand = new RelayCommand<int>(EditFamily);

            LoadFamilies();
        }

        public async void LoadFamilies()
        {
            try
            {
                var familiesFromDb = await _healthRecordService.GetRecordsByHouseID(_idHouse);
                var familiesGrouped = familiesFromDb
                    .Where(p => p.FamilyId > 0)
                    .GroupBy(p => p.FamilyId)
                    .Select(g => new Familia
                    {
                        IdFamily = g.Key,
                        PessoasFamilia = new ObservableCollection<HealthRecord>(g.ToList())
                    }).ToList();

                Families.Clear();
                foreach (var family in familiesGrouped)
                {
                    Families.Add(family);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        private async void AddFamily()
        {
            try
            {
                var newFamily = new Familia
                {
                    PessoasFamilia = new ObservableCollection<HealthRecord>() // Cria uma nova lista de pessoas
                };

                Families.Add(newFamily); // Adiciona a nova família à coleção
                OnPropertyChanged(nameof(Families)); // Notifica a CollectionView para atualizar os dados
                await Application.Current.MainPage.Navigation.PushAsync(new AddFamilyPage(_idHouse, false));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", $"Não foi possível adicionar a nova família.\n\n{ex.Message}", "OK");
            }
        }

        private async void DeleteFamily(int idFamily)
        {
            try
            {
                Console.WriteLine("DeleteFamily acessado.");

                var confirm = await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp(
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

                OnPropertyChanged(nameof(Families)); // Notifica a CollectionView
                LoadFamilies();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
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
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Família não encontrada.", "OK");
                    return;
                }

                // Navegar para a página de edição
                await Application.Current.MainPage.Navigation.PushAsync(new AddFamilyPage(_idHouse, true, idFamily));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", $"Não foi possível editar a família.\n\n{ex.Message}", "OK");
            }
        }
    }
}
