using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class AddFamilyViewModel : BaseViewModel
    {
        private readonly IFamilyService _familyService = App.ServiceProvider.GetRequiredService<IFamilyService>();
        private readonly IFamilyManager _familyManager = App.ServiceProvider.GetRequiredService<IFamilyManager>();
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();

        [ObservableProperty] private ObservableCollection<Pessoa> pessoas = [];
        [ObservableProperty] private ObservableCollection<Pessoa> pessoasPesquisadas = [];

        public ICommand SalvarCommand => new Command(async () => await SalvarFamilia());
        public ICommand AddPersonCommand => new Command<int>(async (id) => await AddPerson(id));
        public ICommand SearchCommand => new Command<string>(async s => await Search(s));
        public ICommand DeleteCommand => new Command<int>(async (id) => await DeletePerson(id));
        public ICommand GoBack => new Command(async () => await Shell.Current.GoToAsync("..", new Dictionary<string, object> { { "id", IdHouse } }));


        public int IdHouse { get; set; }
        public int IdPessoa { get; set; }
        public bool IsEdit { get; }

        private CancellationTokenSource _debounceTimer = new();

        public AddFamilyViewModel(int idHouse, bool isEdit, int? idFamily)
        {
            IdHouse = idHouse;
            IsEdit = isEdit;
            IdPessoa = idFamily ?? 0;

            MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
        }

        private async Task SalvarFamilia()
        {
            if (IdHouse <= 0)
            {
                await MostrarErro("IdHouse inválido. Selecione uma residência.");
                return;
            }

            if (Pessoas.Count == 0)
            {
                await MostrarErro("Família sem membros. Adicione pelo menos uma pessoa.");
                return;
            }

            try
            {
                int familyId = IsEdit ? IdPessoa : await _familyService.GetMaxIdAsync(IdHouse) + 1;
                var susList = Pessoas.Select(p => p.Id).ToList();

                await _familyManager.AddPeopleToFamily(susList, IdHouse, familyId);

                await MostrarSucesso(IsEdit ? "Família atualizada." : "Família criada.");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await MostrarErro($"Erro ao salvar família: {ex.Message}");
            }
        }

        private async Task DeletePerson(int id)
        {
            var pessoa = Pessoas.FirstOrDefault(p => p.Id == id);
            if (pessoa == null)
            {
                await MostrarErro("Pessoa não encontrada.");
                return;
            }

            bool confirm = Convert.ToBoolean(await Shell.Current.ShowPopupAsync(
                new DisplayPopUp("Confirmar", $"Deseja remover {pessoa.Nome}?", true, "Remover", true, "Cancelar")));

            if (!confirm) return;

            Pessoas.Remove(pessoa);
            await _familyManager.RemovePersonFromFamily(id);
        }

        private async Task Search(string termo)
        {
            _debounceTimer?.Cancel();
            _debounceTimer = new CancellationTokenSource();
            await Task.Delay(300, _debounceTimer.Token);

            PessoasPesquisadas.Clear();
            if (string.IsNullOrWhiteSpace(termo)) return;

            var parcial = await _patientService.GetAllPatients();
            var resultados = parcial.Where(n => n.Name.Contains(termo) || 
                                           n.SusNumber.Contains(termo) || 
                                           n.MotherName.Contains(termo) || 
                                           n.FatherName.Contains(termo));

            foreach (var p in resultados)
            {
                PessoasPesquisadas.Add(new Pessoa { Nome = p.Name, Id = p.Id });
            }
        }

        private async Task AddPerson(int id)
        {
            var pessoa = await _patientService.GetPatientById(id);

            if (pessoa != null && !Pessoas.Any(p => p.Id == id))
            {
                Pessoas.Add(new Pessoa { Nome = pessoa.Name, Id = pessoa.Id });
            }
        }

        private async Task LoadDataAsync()
        {
            if (IsEdit)
            {
                //var registros = await _healthRecordService.GetRecordsByFamilyAndHouseAsync(IdPessoa, IdHouse);
                
                var parcial = await _patientService.GetAllPatients();

                var registros = parcial.Where(n => n.FamilyId == IdPessoa && n.HouseId == IdHouse);

                Pessoas.Clear();

                foreach (var r in registros)
                {
                    Pessoas.Add(new Pessoa { Nome = r.Name, Id = r.Id });
                }
            }
        }

        private static async Task MostrarErro(string msg)
        {
            await Shell.Current.ShowPopupAsync(new DisplayPopUp("Erro", msg, true, "Voltar", false, ""));
        }

        private static async Task MostrarSucesso(string msg) => await Shell.Current.ShowPopupAsync(new DisplayPopUp("Sucesso", msg, true, "Voltar", false, ""));
    }
}
