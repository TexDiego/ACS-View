using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class RegistersViewModel : BaseViewModel
    {
        private readonly IVaccineService _vaccineService = App.ServiceProvider.GetRequiredService<IVaccineService>();
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();

        [ObservableProperty] private string condition = "Pacientes";
        [ObservableProperty] private ObservableCollection<Patient?> patients = [];
        [ObservableProperty] private int totalRecords;

        private Vaccines _vaccines;

        public int ScrollToId { get; set; }


        public ICommand DeleteCommand => new Command<int>(async (id) => await DeleteRecordAsync(id));
        public ICommand EditCommand => new Command<Patient>(async (p) => await EditRecordAsync(p));
        public ICommand PersonInfo => new Command<Patient>(async record => await PersonData(record));
        public ICommand Vaccines => new Command<int>(async (id) => await VaccinesPage(id));
        public ICommand AddPerson => new Command(async () => await Shell.Current.GoToAsync("addregister"));
        public ICommand Houses => new Command(async () => await Shell.Current.GoToAsync("//houses"));
        public ICommand Filter => new Command(async () => await Shell.Current.ShowPopupAsync(new FilterPopup()));

        internal async Task LoadPatients()
        {
            Patients.Clear();

            var patients = await _patientService.GetAllPatients();

            foreach (Patient p in patients)
            {
                Patients.Add(p);
            }

            TotalRecords = Patients.Count;
        }

        private async Task<string> GetAddressAsync(int id)
        {
            try
            {
                var house = await _houseService.GetHouseByPatientIdAsync(id);
                if (house == null) return "Sem endereço.";

                string rua = house.Rua ?? "";
                string numeroRua = house.NumeroCasa ?? "";
                string complemento = string.IsNullOrWhiteSpace(house.Complemento) ? "" : $"- {house.Complemento}";

                return $"{rua}, {numeroRua} {complemento}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter endereço: {ex.Message}");
                return "Erro ao buscar endereço.";
            }
        }

        private async Task DeleteRecordAsync(int id)
        {
            var confirm = Convert.ToBoolean(await Shell.Current.ShowPopupAsync(new DisplayPopUp(
                                            "Confirmar",
                                            $"Tem certeza de que deseja excluir o cadastro?",
                                            true, "Excluir", true, "Cancelar")));

            if (confirm) return;

            var record = Patients.FirstOrDefault(r => r.Id == id);
            if (record != null)
            {
                await _patientService.DeletePatient(id);
                Patients.Remove(record);
            }

            TotalRecords = Patients.Count;
        }

        private async Task EditRecordAsync(Patient patient)
        {
            ScrollToId = patient.Id;

            await Shell.Current.GoToAsync("addregister", new Dictionary<string, object> { { "record", patient } });
        }

        private async Task PersonData(Patient record)
        {
            if (record == null) return;

            var popup = new PersonsInfo(record);
            await Shell.Current.ShowPopupAsync(popup);
        }

        private async Task VaccinesPage(int id)
        {
            _vaccines = await _vaccineService.GetVaccinesByIdAsync(id);

            if (_vaccines == null)
            {
                await AddVaccineMissing(id);
                _vaccines = await _vaccineService.GetVaccinesByIdAsync(id);
            }

            ScrollToId = _vaccines.Id;
            var page = new VaccinesPage(_vaccines);
            await Shell.Current.Navigation.PushAsync(page);
        }

        private async Task AddVaccineMissing(int id)
        {
            var record = await _patientService.GetPatientById(id);
            if (record == null) return;

            var vaccine = new Vaccines
            {
                Id = record.Id,
                BirthDate = record.BirthDate
            };

            await _vaccineService.AdicionarVacinasAsync(vaccine);
        }
    }
}