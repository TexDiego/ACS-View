using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class AddFamilyViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly FamilyService _familyService;
        private readonly HealthRecordService _healthRecordService;

        private List<HealthRecord> peopleToAdd = [];

        private readonly ObservableCollection<Pessoa> _pessoasPesquisada = [];
        public ObservableCollection<Pessoa> PessoasPesquisadas => _pessoasPesquisada;


        private readonly ObservableCollection<Pessoa> _pessoas = [];
        public ObservableCollection<Pessoa> Pessoas => _pessoas;
        public ObservableCollection<HealthRecord> _healthRecord { get; private set; }


        private readonly ObservableCollection<Familia> _familia = [];
        public ObservableCollection<Familia> Familia => _familia;
        public ObservableCollection<Family> _family { get; private set; }

        public int IdHouse { get; set; }
        public int IdPessoa { get; set; }
        public bool IsEdit;

        public ICommand SalvarCommand { get; }
        public ICommand AddPersonCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }

        public AddFamilyViewModel() { }

        public AddFamilyViewModel(int id, bool isEdit, int? idFamily) : base()
        {
            _databaseService = new();
            _familyService = new FamilyService(_databaseService);
            _healthRecordService = new HealthRecordService(_databaseService);
            _healthRecord = [];

            IdHouse = id;
            IsEdit = isEdit;

            if (isEdit)
            {
                IdPessoa = idFamily.Value;
            }

            _family = [];
            _familia = [];

            DeleteCommand = new Command<string>(DeletePerson);
            SalvarCommand = new Command(SalvarFamilia);
            SearchCommand = new Command<string>(Search);
            AddPersonCommand = new Command<string>(AddPerson);

            _ = LoadDataAsync();
        }

        private async void SalvarFamilia()
        {
            Console.WriteLine("SalvarFamilia acessado\n\n");

            if (IdHouse <= 0)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", "IdHouse inválido. Por favor, selecione uma residência.", true, "Voltar", false, ""));
                return;
            }

            try
            {
                Console.WriteLine($"Id pessoa: {IdPessoa}\nIdHouse: {IdHouse}");

                var num = _pessoas.Count;
                var pessoas = _pessoas.ToList();

                if (num == 0)
                {
                    await Application.Current.MainPage.ShowPopupAsync(
                        new DisplayPopUp("Família sem membros", "Não é possível criar ou atualizar uma família sem membros.", true, "Voltar", false, ""));
                    return;
                }

                if (IsEdit)
                {
                    // Editar a família existente - NÃO ALTERAR FamilyId
                    foreach (var p in pessoas)
                    {
                        var pessoaAtualizar = await _healthRecordService.GetRecordBySusAsync(p.Sus);

                        if (pessoaAtualizar != null)
                        {
                            pessoaAtualizar.HouseId = IdHouse;
                            pessoaAtualizar.FamilyId = IdPessoa;
                            Console.WriteLine($"Atualizando Pessoa: SUS={pessoaAtualizar.SusNumber}, FamilyID={pessoaAtualizar.FamilyId}, HouseID={pessoaAtualizar.HouseId}");
                            await _healthRecordService.UpdateRecordAsync(pessoaAtualizar);
                        }
                    }

                    await Application.Current.MainPage.ShowPopupAsync(
                        new DisplayPopUp("Sucesso!", "Família atualizada.", true, "Voltar", false, ""));
                }
                else
                {
                    // Criar nova família
                    var newFamilyId = await _familyService.GetMaxIdAsync(IdHouse) + 1; // Obtenha o próximo FamilyId
                    Console.WriteLine($"Novo ID Familia: {newFamilyId}");

                    foreach (var p in pessoas)
                    {
                        var pessoaAtualizar = await _healthRecordService.GetRecordBySusAsync(p.Sus);

                        if (pessoaAtualizar != null)
                        {
                            pessoaAtualizar.FamilyId = newFamilyId; // Utiliza o novo FamilyId
                            pessoaAtualizar.HouseId = IdHouse;

                            Console.WriteLine($"Criando Pessoa: SUS={pessoaAtualizar.SusNumber}, FamilyID={pessoaAtualizar.FamilyId}, HouseID={pessoaAtualizar.HouseId}");
                            await _healthRecordService.UpdateRecordAsync(pessoaAtualizar);
                        }
                    }

                    await Application.Current.MainPage.ShowPopupAsync(
                        new DisplayPopUp("Sucesso!", "Família adicionada à residência.", true, "Voltar", false, ""));
                }

                // Limpar campos e atualizar dados
                LimparCampos();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }

        private async void DeletePerson(string sus)
        {
            try
            {
                var pessoa = _pessoas.FirstOrDefault(p => p.Sus == sus);
                var p = await _healthRecordService.GetRecordBySusAsync(sus);

                if (pessoa == null)
                {
                    await Application.Current.MainPage.ShowPopupAsync(
                        new DisplayPopUp("Erro", "Pessoa não encontrada.", true, "Voltar", false, ""));
                    return;
                }

                bool confirm = Convert.ToBoolean(await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Confirmar", $"Tem certeza de que deseja remover {pessoa.Nome}?", true, "Remover", true, "Cancelar")));

                if (!confirm)
                {
                    // Remover da lista _pessoas
                    _pessoas.Remove(pessoa);

                    // Atualizar a família e a pessoa no banco de dados
                    p.FamilyId = 0;
                    p.HouseId = 0;

                    await _healthRecordService.UpdateRecordAsync(p);

                    // Notificar a interface do usuário após a remoção
                    OnPropertyChanged(nameof(Pessoas));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", $"Não foi possível remover o membro da família.\n\n{ex.Message}", true, "Voltar", false, ""));
            }
        }

        private async void Search(string search)
        {
            try
            {
                var pesquisa = await _healthRecordService.GetRecordByNameOrSus(search);
                Console.WriteLine(pesquisa.Count);

                _pessoasPesquisada.Clear();

                if (pesquisa != null && search != null)
                {
                    foreach (var pessoa in pesquisa)
                    {
                        _pessoasPesquisada.Add(new Pessoa
                        {
                            Nome = pessoa.Name,
                            Sus = pessoa.SusNumber
                        });

                        Console.WriteLine($"Nome: {pessoa.Name}\nSUS: {pessoa.SusNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async void AddPerson(string sus)
        {
            try
            {
                var familyMember = await _healthRecordService.GetRecordBySusAsync(sus);

                Console.WriteLine($"IdPessoa: {IdPessoa}\nIdHouse: {IdHouse}\nSUS: {sus}\n");

                if (familyMember != null && !peopleToAdd.Contains(familyMember))
                {
                    _pessoas.Add(new Pessoa
                    {
                        Nome = familyMember.Name,
                        Sus = familyMember.SusNumber
                    });

                    peopleToAdd.Add(familyMember);

                    Console.WriteLine(familyMember.Name);
                }

                OnPropertyChanged(nameof(Pessoas));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (IsEdit)
                {
                    var familyPeople = await _healthRecordService.GetRecordsByFamilyAndHouseAsync(IdPessoa, IdHouse);
                    _pessoas.Clear();

                    foreach (var familyPerson in familyPeople)
                    {
                        _pessoas.Add(new Pessoa
                        {
                            Nome = familyPerson.Name,
                            Sus = familyPerson.SusNumber
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.ShowPopupAsync(
                    new DisplayPopUp("Erro", $"Não foi possível carregar os dados.\n\n{ex.Message}", true, "Voltar", false, ""));
            }
        }

        private void LimparCampos()
        {
            IdHouse = 0;
        }
    }
}
