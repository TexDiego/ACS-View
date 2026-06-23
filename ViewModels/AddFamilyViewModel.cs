using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class AddFamilyViewModel : BaseViewModel
    {
        private readonly IFamilyService _familyService;
        private readonly IFamilyManager _familyManager;
        private readonly IPatientService _patientService;

        [ObservableProperty] private ObservableCollection<Pessoa> pessoas = [];
        [ObservableProperty] private ObservableCollection<Pessoa> pessoasPesquisadas = [];
        [ObservableProperty] private bool hasSearchResults;

        public ICommand SalvarCommand => new Command(async () => await SalvarFamilia());
        public ICommand AddPersonCommand => new Command<int>(async (id) => await AddPerson(id));
        public ICommand SearchCommand => new Command<string>(async s => await SearchAsync(s));
        public ICommand DeleteCommand => new Command<int>(async (id) => await DeletePerson(id));
        public ICommand GoBack => new Command(async () => await NavigateBackAsync(new Dictionary<string, object> { { "id", IdHouse } }));


        public int IdHouse { get; set; }
        public int IdPessoa { get; set; }
        public bool IsEdit { get; }
        private int _loadedVersion = -1;
        private bool _hasLoaded;

        private CancellationTokenSource _debounceTimer = new();

        public AddFamilyViewModel(
            int idHouse,
            bool isEdit,
            int? idFamily,
            IFamilyService familyService,
            IFamilyManager familyManager,
            IPatientService patientService)
        {
            IdHouse = idHouse;
            IsEdit = isEdit;
            IdPessoa = idFamily ?? 0;
            _familyService = familyService;
            _familyManager = familyManager;
            _patientService = patientService;
        }

        private async Task SalvarFamilia()
        {
            if (IdHouse <= 0)
            {
                await DisplayAlertAsync("Erro", "IdHouse inválido. Selecione uma residência.", "Voltar");
                return;
            }

            if (Pessoas.Count == 0)
            {
                await DisplayAlertAsync("Erro", "Família sem membros. Adicione ao menos uma pessoa.", "Voltar");
                return;
            }

            try
            {
                int familyId = IsEdit ? IdPessoa : await _familyService.GetMaxIdAsync(IdHouse) + 1;
                var susList = Pessoas.Select(p => p.Id).ToList();

                await _familyManager.AddPeopleToFamily(susList, IdHouse, familyId);

                if (IsEdit)
                    await DisplayAlertAsync("Sucesso!", "Família atualizada.", "Voltar");
                else
                    await DisplayAlertAsync("Sucesso!", "Família criada.", "Voltar");
                await NavigateBackAsync();
            }
            catch (Exception)
            {
                await DisplayAlertAsync("Erro", "Erro ao salvar família.", "Voltar");
            }
        }

        private async Task DeletePerson(int id)
        {
            var pessoa = Pessoas.FirstOrDefault(p => p.Id == id);
            if (pessoa == null)
            {
                await DisplayAlertAsync("Erro", "Pessoa não encontrada.", "Voltar");
                return;
            }

            bool confirm = await DisplayConfirmationAsync(
                "Confirmar",
                $"Deseja remover {pessoa.Nome}?",
                "Remover");

            if (!confirm) return;

            Pessoas.Remove(pessoa);
            await _familyManager.RemovePersonFromFamily(id);
        }

        public async Task SearchAsync(string termo)
        {
            _debounceTimer?.Cancel();
            _debounceTimer = new CancellationTokenSource();
            var token = _debounceTimer.Token;

            var normalizedTerm = termo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedTerm))
            {
                PessoasPesquisadas.Clear();
                HasSearchResults = false;
                return;
            }

            await Task.Delay(300, token);
            token.ThrowIfCancellationRequested();

            var parcial = await _patientService.GetAllPatients() ?? [];
            token.ThrowIfCancellationRequested();

            var resultados = parcial
                .Where(n => ContainsTerm(n.Name, normalizedTerm) ||
                            ContainsTerm(n.SusNumber, normalizedTerm) ||
                            ContainsTerm(n.MotherName, normalizedTerm) ||
                            ContainsTerm(n.FatherName, normalizedTerm))
                .Where(n => !Pessoas.Any(p => p.Id == n.Id))
                .OrderBy(n => n.Name)
                .Take(20)
                .Select(p => new Pessoa { Nome = p.Name, Id = p.Id })
                .ToList();

            RunOnMainThread(() =>
            {
                PessoasPesquisadas = new ObservableCollection<Pessoa>(resultados);
                HasSearchResults = PessoasPesquisadas.Count > 0;
            });
        }

        private async Task AddPerson(int id)
        {
            var pessoa = await _patientService.GetPatientById(id);

            if (pessoa != null && !Pessoas.Any(p => p.Id == id))
            {
                Pessoas.Add(new Pessoa { Nome = pessoa.Name, Id = pessoa.Id });
            }

            var searchedPerson = PessoasPesquisadas.FirstOrDefault(p => p.Id == id);
            if (searchedPerson != null)
            {
                PessoasPesquisadas.Remove(searchedPerson);
                HasSearchResults = PessoasPesquisadas.Count > 0;
            }
        }

        private static bool ContainsTerm(string? value, string term)
        {
            var normalizedValue = SearchTextNormalizer.Normalize(value);
            var normalizedTerm = SearchTextNormalizer.Normalize(term);

            return !string.IsNullOrWhiteSpace(value) &&
                   normalizedValue.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
        }

        public async Task LoadDataAsync()
        {
            if (IsEdit)
            {
                if (_hasLoaded && _loadedVersion == DataChangeTracker.PatientsVersion)
                {
                    return;
                }

                var registros = await _patientService.GetPatientsByFamilyAndHouseId(IdPessoa, IdHouse) ?? [];

                Pessoas.Clear();

                foreach (var r in registros)
                {
                    Pessoas.Add(new Pessoa { Nome = r.Name, Id = r.Id });
                }

                _loadedVersion = DataChangeTracker.PatientsVersion;
                _hasLoaded = true;
            }
        }
    }
}
