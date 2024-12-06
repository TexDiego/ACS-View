using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;

namespace ACS_View.MVVM.Views;

public partial class AddRegister : ContentPage
{
    private readonly AddRegisterViewModel _addRegisterViewModel;

    public AddRegister()
    {
        InitializeComponent();

        // Inicializando as dependências e a ViewModel apenas uma vez
        var databaseService = new DatabaseService();
        var healthRecordService = new HealthRecordService(databaseService);
        _addRegisterViewModel = new AddRegisterViewModel(healthRecordService);

        // Definindo a ViewModel como BindingContext
        BindingContext = _addRegisterViewModel;
        Entry_Birth.MinimumDate = DateTime.Today.AddYears(-120);
        Entry_Birth.MaximumDate = DateTime.Today;
        Entry_Birth.Date = DateTime.Today;
    }

    public AddRegister(HealthRecord healthRecord)
    {
        InitializeComponent();

        // Inicializando as dependências e a ViewModel apenas uma vez
        var databaseService = new DatabaseService();
        var healthRecordService = new HealthRecordService(databaseService);
        _addRegisterViewModel = new AddRegisterViewModel(healthRecordService);

        // Definindo a ViewModel como BindingContext
        BindingContext = _addRegisterViewModel;

        Entry_Name.Text = healthRecord.Name;
        Entry_Sus.Text = healthRecord.SusNumber;
        Entry_Birth.Date = healthRecord.BirthDate;
        Entry_Obs.Text = healthRecord.Observacao;
        CB_Acamado.IsChecked = healthRecord.IsHomebound;
        CB_DB.IsChecked = healthRecord.HasDiabetes;
        CB_Domiciliado.IsChecked = healthRecord.IsBedridden;
        CB_Gestante.IsChecked = healthRecord.IsPregnant;
        CB_HAN.IsChecked = healthRecord.HasLeprosy;
        CB_HAS.IsChecked = healthRecord.HasHypertension;
        CB_Mental.IsChecked = healthRecord.HasMentalIllness;
        CB_TB.IsChecked = healthRecord.HasTuberculosis;
        CB_Disability.IsChecked = healthRecord.HasDisabilities;
        CB_Smoker.IsChecked = healthRecord.IsSmoker;
        CB_Cancer.IsChecked = healthRecord.HasCancer;

        Entry_Birth.MinimumDate = DateTime.Today.AddYears(-120);
        Entry_Birth.MaximumDate = DateTime.Today;
    }

    private async void Btn_AddGoBack_Clicked(object sender, EventArgs e)
    {
        try
        {
            _addRegisterViewModel.IsLoading = true;

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "Voltar");
        }
        finally
        {
            _addRegisterViewModel.IsLoading = false;
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (String.IsNullOrEmpty(Entry_Name.Text) || String.IsNullOrEmpty(Entry_Sus.Text))
            {
                await DisplayAlert("Ops", "Nome e SUS devem ser preenchidos", "Ok");
                return;
            }

            if (Entry_Sus.Text.Length != 15)
            {
                await DisplayAlert("Ops", "O número do SUS deve conter 15 dígitos", "Ok");
                return;
            }

            // Evitar referência nula
            if (_addRegisterViewModel == null)
            {
                await DisplayAlert("Erro", "ViewModel não foi inicializada corretamente.", "OK");
                return;
            }

            // Atribuindo valores aos campos da ViewModel
            _addRegisterViewModel.Nome = Entry_Name.Text.ToUpper();
            _addRegisterViewModel.NumeroSUS = Entry_Sus.Text;
            _addRegisterViewModel.Nascimento = Entry_Birth.Date;
            _addRegisterViewModel.Observacao = string.IsNullOrEmpty(Entry_Obs.Text) ? null : Entry_Obs.Text;
            _addRegisterViewModel.Tuberculose = CB_TB.IsChecked;
            _addRegisterViewModel.Diabetes = CB_DB.IsChecked;
            _addRegisterViewModel.Acamado = CB_Acamado.IsChecked;
            _addRegisterViewModel.Domiciliado = CB_Domiciliado.IsChecked;
            _addRegisterViewModel.Gestante = CB_Gestante.IsChecked;
            _addRegisterViewModel.Hanseniase = CB_HAN.IsChecked;
            _addRegisterViewModel.Hipertensao = CB_HAS.IsChecked;
            _addRegisterViewModel.Deficiente = CB_Disability.IsChecked;
            _addRegisterViewModel.Fumante = CB_Smoker.IsChecked;
            _addRegisterViewModel.Cancer = CB_Cancer.IsChecked;
            _addRegisterViewModel.HasObs = !string.IsNullOrEmpty(Entry_Obs.Text);

            Entry_Sus.Unfocus();
            Entry_Name.Unfocus();
            Entry_Obs.Unfocus();

            // Executa o comando de salvamento
            if (_addRegisterViewModel.SalvarCommand.CanExecute(null))
            {
                _addRegisterViewModel.SalvarCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro inesperado", ex.TargetSite.ToString(), "Voltar");
        }
    }
}
