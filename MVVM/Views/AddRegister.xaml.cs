using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;
using System.Text.RegularExpressions;

namespace ACS_View.MVVM.Views;

public partial class AddRegister : ContentPage
{
    private readonly AddRegisterViewModel _addRegisterViewModel;
    private readonly DatabaseService _databaseService;
    int? HouseId = 0;
    int? FamilyId = 0;

    public AddRegister(DatabaseService databaseService)
    {
        InitializeComponent();

        // Inicializando as dependências e a ViewModel apenas uma vez
        _databaseService = databaseService;
        var healthRecordService = new HealthRecordService(databaseService);
        _addRegisterViewModel = new AddRegisterViewModel(healthRecordService);

        // Definindo a ViewModel como BindingContext
        BindingContext = _addRegisterViewModel;
        Entry_Birth.MinimumDate = DateTime.Today.AddYears(-120);
        Entry_Birth.MaximumDate = DateTime.Today;
        Entry_Birth.Date = DateTime.Today;
    }

    public AddRegister(HealthRecord healthRecord, DatabaseService databaseService, int houseId, int familyId)
    {
        InitializeComponent();

        // Inicializando as dependências e a ViewModel apenas uma vez
        _databaseService = databaseService;
        var healthRecordService = new HealthRecordService(databaseService);
        _addRegisterViewModel = new AddRegisterViewModel(healthRecordService);

        // Definindo a ViewModel como BindingContext
        BindingContext = _addRegisterViewModel;

        Entry_Name.Text = healthRecord.Name;
        Entry_MotherName.Text = healthRecord.MotherName;
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
        HouseId = houseId;
        FamilyId = familyId;

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
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
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
                await this.ShowPopupAsync(new DisplayPopUp("Ops", "Nome e SUS devem ser preenchidos", false, "", true, "Ok"));
                return;
            }

            if (Entry_Sus.Text.Length != 15)
            {
                await this.ShowPopupAsync(new DisplayPopUp("Ops", "O número do SUS deve conter 15 dígitos", false, "", true, "Ok"));
                return;
            }

            // Evitar referência nula
            if (_addRegisterViewModel == null)
            {
                await this.ShowPopupAsync(new DisplayPopUp("Erro", "ViewModel não foi iniciada corretamente", true, "Voltar", false, ""));
                return;
            }

            // Atribuindo valores aos campos da ViewModel
            _addRegisterViewModel.Nome = Entry_Name.Text.ToUpper();
            _addRegisterViewModel.MotherName = Entry_MotherName.Text.ToUpper();
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
            _addRegisterViewModel.HouseId = HouseId ?? 0;
            _addRegisterViewModel.FamilyId = FamilyId ?? 0;

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
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private bool isTextChanging = false; // Flag para evitar loops no evento TextChanged

    private async void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (isTextChanging) return;

        try
        {
            // Define a flag para evitar loops
            isTextChanging = true;

            // Remove múltiplos espaços consecutivos
            var correctedText = Regex.Replace(e.NewTextValue ?? string.Empty, @"\s+", " ");

            // Remove caracteres inválidos (apenas letras e espaços permitidos)
            correctedText = Regex.Replace(correctedText, @"[^a-zA-Z\s]", string.Empty);

            // Garante que não haja espaço inicial
            correctedText = correctedText.TrimStart();

            // Atualiza o texto apenas se houve alteração
            if (correctedText != e.NewTextValue)
            {
                ((Entry)sender).Text = correctedText;
            }
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", "Erro na inserção de caracter inválido\n\n" + ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            // Reseta a flag
            isTextChanging = false;
        }
    }

    private void Entry_Sus_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Regex para permitir apenas números
        var regex = new Regex("^[0-9]*$");

        if (!string.IsNullOrEmpty(e.NewTextValue) && !regex.IsMatch(e.NewTextValue))
        {
            // Reverte para o texto anterior caso contenha caracteres inválidos
            ((Entry)sender).Text = e.OldTextValue;
        }
    }
}