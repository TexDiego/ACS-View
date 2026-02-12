using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class AddFamilyViewModel : BaseViewModel
    {
        private readonly IFamilyService _familyService = App.ServiceProvider.GetRequiredService<IFamilyService>();
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly IFamilyManager _familyManager = App.ServiceProvider.GetRequiredService<IFamilyManager>();

        public ObservableCollection<Pessoa> Pessoas { get; } = [];
        public ObservableCollection<Pessoa> PessoasPesquisadas { get; } = [];

        public ICommand SalvarCommand { get; }
        public ICommand AddPersonCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }

        public int IdHouse { get; set; }
        public int IdPessoa { get; set; }
        public bool IsEdit { get; }

        private CancellationTokenSource _debounceTimer = new();

        public AddFamilyViewModel(int idHouse, bool isEdit, int? idFamily)
        {
            IdHouse = idHouse;
            IsEdit = isEdit;
            IdPessoa = idFamily ?? 0;

            DeleteCommand = new Command<string>(DeletePerson);
            SalvarCommand = new Command(async () => await SalvarFamilia());
            SearchCommand = new Command<string>(async s => await Search(s));
            AddPersonCommand = new Command<string>(async s => await AddPerson(s));

            _ = LoadDataAsync();
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
                var susList = Pessoas.Select(p => p.Sus).ToList();

                await _familyManager.AddPeopleToFamily(susList, IdHouse, familyId);

                await MostrarSucesso(IsEdit ? "Família atualizada." : "Família criada.");
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await MostrarErro($"Erro ao salvar família: {ex.Message}");
            }
        }

        private async void DeletePerson(string sus)
        {
            var pessoa = Pessoas.FirstOrDefault(p => p.Sus == sus);
            if (pessoa == null)
            {
                await MostrarErro("Pessoa não encontrada.");
                return;
            }

            bool confirm = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(
                new DisplayPopUp("Confirmar", $"Deseja remover {pessoa.Nome}?", true, "Remover", true, "Cancelar")));

            if (!confirm) return;

            Pessoas.Remove(pessoa);
            await _familyManager.RemovePersonFromFamily(sus);
        }

        private async Task Search(string termo)
        {
            _debounceTimer?.Cancel();
            _debounceTimer = new CancellationTokenSource();
            await Task.Delay(300, _debounceTimer.Token);

            PessoasPesquisadas.Clear();
            if (string.IsNullOrWhiteSpace(termo)) return;

            var resultados = await _healthRecordService.GetRecordByNameOrSusAsync(termo);
            foreach (var p in resultados)
            {
                PessoasPesquisadas.Add(new Pessoa { Nome = p.Name, Sus = p.SusNumber });
            }
        }

        private async Task AddPerson(string sus)
        {
            var pessoa = await _healthRecordService.GetRecordBySusAsync(sus);
            if (pessoa != null && !Pessoas.Any(p => p.Sus == sus))
            {
                Pessoas.Add(new Pessoa { Nome = pessoa.Name, Sus = pessoa.SusNumber });
            }
        }

        private async Task LoadDataAsync()
        {
            if (IsEdit)
            {
                var registros = await _healthRecordService.GetRecordsByFamilyAndHouseAsync(IdPessoa, IdHouse);
                Pessoas.Clear();
                foreach (var r in registros)
                {
                    Pessoas.Add(new Pessoa { Nome = r.Name, Sus = r.SusNumber });
                }
            }
        }

        private static async Task MostrarErro(string msg)
        {
            await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", msg, true, "Voltar", false, ""));
        }

        private static async Task MostrarSucesso(string msg) => await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Sucesso", msg, true, "Voltar", false, ""));
    }
}
