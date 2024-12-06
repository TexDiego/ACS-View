using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModel
{
    public class RegistersViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;
        private List<HealthRecord> _healthRecords = new();
        private readonly ObservableCollection<Pessoa> _person = new();
        public ObservableCollection<Pessoa> Person => _person;
        public ObservableCollection<HealthRecord> _healthRecord { get; private set; }
        public ICommand DeleteCommand { get; }
        public ICommand EditCommand { get; }

        public RegistersViewModel() { Console.WriteLine("vm acessada"); }

        public RegistersViewModel(HealthRecordService healthRecordService, string condition, string search)
        {
            _healthRecordService = healthRecordService ?? throw new ArgumentNullException(nameof(healthRecordService));
            LoadHealthRecordsAndUpdateDatasAsync(condition, search).ConfigureAwait(false); // Evitar deadlocks

            _healthRecord = new ObservableCollection<HealthRecord>();

            DeleteCommand = new Command<string>(async susNumber =>
            {
                try
                {
                    bool result = await Application.Current.MainPage.DisplayAlert("Confirmar", "Tem certeza de que deseja excluir o cadastro? SUS: " + susNumber, "Excluir", "Cancelar");
                    if (!result) return;

                    var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
                    if (record != null)
                    {
                        // Exclusão do banco de dados
                        await _healthRecordService.DeleteRecordAsync(record.SusNumber);

                        // Remover da lista interna (sem vínculo com a UI)
                        _healthRecords.Remove(record);

                        // Remover da ObservableCollection que está vinculada à UI
                        var personToRemove = _person.FirstOrDefault(p => p.Sus == susNumber);
                        if (personToRemove != null)
                        {
                            _person.Remove(personToRemove);
                        }

                        // Atualiza a lista visível na UI
                        UpdateDatas(condition, search);
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Erro", $"Não foi possível deletar o registro.\n\n{ex.Message}", "OK");
                }
            });

            EditCommand = new Command<string>(async (susNumber) =>
            {
                var record = _healthRecords.FirstOrDefault(r => r.SusNumber == susNumber);
                if (record != null)
                {
                    // Navegar para a página de edição e passar os dados do registro
                    await Application.Current.MainPage.Navigation.PushAsync(new AddRegister(record));
                }
            });
        }

        private async Task LoadHealthRecordsAndUpdateDatasAsync(string condition, string search)
        {
            try
            {
                _healthRecords = await _healthRecordService.GetAllRecordsAsync();
                UpdateDatas(condition, search);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar registros: {ex.Message}");
            }
        }


        public void UpdateDatas(string condition, string search)
        {
            try
            {
                // Filtra os registros
                var filteredRecords = FilterRecords(condition, search);

                // Atualiza a ObservableCollection com os resultados filtrados
                _person.Clear();

                foreach (var record in filteredRecords)
                {
                    _person.Add(new Pessoa
                    {
                        Nome = record.Name,
                        Sus = record.SusNumber,
                        Observacao = record.Observacao,
                        HasObs = record.HasObs,
                        Nascimento = record.BirthDate,
                        Idade = CalcularIdadeCompleta(record.BirthDate)
                    });
                }

                OnPropertyChanged(nameof(Person));
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "Voltar");
            }
        }

        public static string CalcularIdadeCompleta(DateTime dataNascimento)
        {
            DateTime hoje = DateTime.Today;

            int anos = hoje.Year - dataNascimento.Year;
            int meses = hoje.Month - dataNascimento.Month;
            int dias = hoje.Day - dataNascimento.Day;

            // Ajustar meses e dias, se necessário
            if (dias < 0)
            {
                meses--;
                dias += DateTime.DaysInMonth(hoje.AddMonths(-1).Year, hoje.AddMonths(-1).Month);
            }

            if (meses < 0)
            {
                anos--;
                meses += 12;
            }

            // Casos especiais
            if (anos == 0 && meses == 0 && dias == 0)
                return "Recém-nascido";

            if (anos == 0 && meses == 0)
                return dias == 1 ? "1 dia" : $"{dias} dias";

            if (anos == 0)
                return meses == 1 ? "1 mês" : $"{meses} meses";

            if (anos == 1)
                return meses == 0 ? "1 ano" : meses == 1 ? "1 ano e 1 mês" : $"1 ano e {meses} meses";

            return $"{anos} anos";
        }



        private List<HealthRecord> FilterRecords(string condition, string search)
        {
            IEnumerable<HealthRecord> filteredRecords = condition switch
            {
                "GESTANTE" => _healthRecords.Where(r => r.IsPregnant),
                "DB" => _healthRecords.Where(r => r.HasDiabetes),
                "HAS" => _healthRecords.Where(r => r.HasHypertension),
                "HASDB" => _healthRecords.Where(r => r.IsDiabetesAndHypertension),
                "TB" => _healthRecords.Where(r => r.HasTuberculosis),
                "HAN" => _healthRecords.Where(r => r.HasLeprosy),
                "ACAMADO" => _healthRecords.Where(r => r.IsHomebound),
                "DOMICILIADO" => _healthRecords.Where(r => r.IsBedridden),
                "MENOR" => _healthRecords.Where(r => r.IsBaby),
                "MENTAL" => _healthRecords.Where(r => r.HasMentalIllness),
                "DEFICIENTE" => _healthRecords.Where(r => r.HasDisabilities),
                "FUMANTE" => _healthRecords.Where(r => r.IsSmoker),
                "CANCER" => _healthRecords.Where(r => r.HasCancer),
                "IDOSO" => _healthRecords.Where(r => r.IsOld),
                _ => _healthRecords,
            };

            return filteredRecords.Where(r => r.Name.ToLower().Normalize().Contains(search.ToLower().Normalize().TrimStart().TrimEnd())
                                           || r.SusNumber.Contains(search.TrimStart().TrimEnd())).ToList();
        }
    }
}
