using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class FamiliesViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly DatabaseService _databaseService;
        private readonly VisitsService _visitsService;

        public ObservableCollection<Familia> Families { get; } = new ObservableCollection<Familia>();
        private HouseService _houseService;
        private string _house;
        public string House
        {
            get => _house;
            set
            {
                _house = value;
                OnPropertyChanged(nameof(House));
            }
        }

        public IRelayCommand AddFamilyCommand { get; }
        public IRelayCommand<int> DeleteFamilyCommand { get; }
        public IRelayCommand<int> EditFamilyCommand { get; }
        public IRelayCommand<int> VisitFamilyCommand { get; }
        public ICommand PersonInfo { get; }


        public int _idHouse;

        public FamiliesViewModel() { }
        public FamiliesViewModel(int idHouse)
        {
            _idHouse = idHouse;
            _databaseService = new DatabaseService();
            _healthRecordService = new HealthRecordService(_databaseService);
            _houseService = new HouseService(_databaseService);
            _visitsService = new VisitsService(_databaseService);

            AddFamilyCommand = new RelayCommand(AddFamily);
            DeleteFamilyCommand = new RelayCommand<int>(DeleteFamily);
            EditFamilyCommand = new RelayCommand<int>(EditFamily);
            PersonInfo = new Command<string>(async susNumber => await PersonData(susNumber));
            VisitFamilyCommand = new RelayCommand<int>(VisitFamily);

            LoadFamilies();
        }

        public async void LoadFamilies()
        {
            try
            {
                var house = await _houseService.GetHousesById(_idHouse);
                string rua = house.Rua ?? "Famílias";
                string numero = house.NumeroCasa ?? "";
                string complemento = house.Complemento ?? "";

                House = complemento == "" ? $"{rua}\n Nº {numero}" : $"{rua}\n Nº {numero} - {complemento}";

                Console.WriteLine(House);

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

        public async void VisitFamily(int idFamily)
        {
            try
            {
                var familyToVisit = Families.FirstOrDefault(f => f.IdFamily == idFamily);

                if (familyToVisit == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Família não encontrada.", "OK");
                    return;
                }

                var visit = await Application.Current.MainPage.ShowPopupAsync(new VisitPage(_idHouse, idFamily));

                if (visit is Visits)
                {
                    await _visitsService.RegisterVisitAsync((Visits)visit);
                    await Application.Current.MainPage.DisplayAlert("Sucesso", "Visita realizada", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Visita não registrada.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", $"Não foi possível visitar a família.\n\n{ex.Message}", "OK");
            }
        }

        private async Task PersonData(string susNumber)
        {
            try
            {
                var record = await _healthRecordService.GetRecordBySusAsync(susNumber);

                if (record != null)
                {
                    var popup = new PersonsInfo(record, _databaseService);
                    await popup.LoadAddressAsync();
                    await Application.Current.MainPage.ShowPopupAsync(popup);
                }
            }
            catch
            {
                await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", "Erro ao carregar os dados da pessoa.", true, "Fechar", false, ""));
            }
        }
    }
}
