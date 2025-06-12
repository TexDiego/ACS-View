using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class OverallView : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly OverallViewModel _overallViewModel;
    private readonly HealthRecordService _healthRecordService;
    private readonly VaccineService _vaccineService;
    private readonly AddRegisterViewModel _addRegisterViewModel;
    private readonly AddHouseViewModel _addHouseViewModel;
    private readonly HouseService _houseService;
    private readonly NoteService _noteService;
    private readonly NotesPageViewModel _notesPageViewModel;

    public OverallView()
    {
        InitializeComponent();

        _dbService = new DatabaseService();
        _noteService = new NoteService(_dbService);
        _healthRecordService = new HealthRecordService(_dbService);
        _vaccineService = new VaccineService(_dbService);

        _houseService = new HouseService(_dbService);
        _overallViewModel = new OverallViewModel(_healthRecordService, _houseService);
        _addRegisterViewModel = new AddRegisterViewModel(_healthRecordService, _vaccineService);
        _addHouseViewModel = new AddHouseViewModel(_houseService);
        _notesPageViewModel = new NotesPageViewModel(_noteService);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _overallViewModel;
        _overallViewModel.AtualizarContagens();
    }

    private async void Btn_OverallViewExit_Clicked(object sender, EventArgs e)
    {
        var answer = await this.ShowPopupAsync(new DisplayPopUp("Sair", "Deseja desconectar-se?", true, "Sair", true, "Cancelar"));

        if (!Convert.ToBoolean(answer))
        {
            await Navigation.PopAsync();
        }
    }

    private async void Btn_OverallAdd_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddRegister(_dbService));
    }

    #region conditions buttons

    private void Btn_Gestantes_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIGestantes, "GESTANTE");
    }

    private async void Btn_Casas_Clicked(object sender, EventArgs e)
    {
        try
        {
            AICasas.IsVisible = true;
            _overallViewModel.IsLoading = true;
            await Navigation.PushAsync(new HousesPage(_dbService, _houseService));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            _overallViewModel.IsLoading = false; // Desativa o indicador de carregamento
            AICasas.IsVisible = false;
        }
    }

    private void Btn_NoResidence_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIHAS, "NOHOME");
    }

    private void Btn_HAS_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIHAS, "HAS");
    }

    private void Btn_DB_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIDB, "DB");
    }

    private void Btn_HASDB_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIHASDB, "HASDB");
    }

    private void Btn_HAN_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIHAN, "HAN");
    }

    private void Btn_TB_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AITB, "TB");
    }

    private void Btn_Acamados_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIDOM, "DOMICILIADO");
    }

    private void Btn_Domiciliados_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIACA, "ACAMADO");
    }

    private void Btn_Menor6Anos_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIMENOR, "MENOR");
    }

    private void Btn_Mental_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIMENTAL, "MENTAL");
    }

    private void Btn_Fumante_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIFUMANTE, "FUMANTE");
    }

    private void Btn_Alcoolatra_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIALCOOLATRA, "ALCOOLATRA");
    }

    private void Btn_Deficiente_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIDEFICIENTE, "DEFICIENTE");
    }

    private void Btn_Total_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AITotal, "TODOS");
    }

    private void Btn_Cancer_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AICANCER, "CANCER");
    }

    private void Btn_Old_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIOLD, "IDOSO");
    }

    private void Btn_Bolsa_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIBOLSA, "BOLSA");
    }

    private void Btn_Cardiaco_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIBOLSA, "CORACAO");
    }

    private void Btn_Kidney_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIKIDNEY, "RIM");
    }

    private void Btn_Liver_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AILIVER, "FIGADO");
    }

    private void Btn_Lungs_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AILUNGS, "PULMAO");
    }

    private void Btn_Neurodivergents_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AINEURODIVERGENTS, "NEURO");
    }

    private void Btn_Addicted_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIDEPENDENTES, "DROGAS");
    }

    private void Btn_Hiv_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIHIV, "HIV");
    }

    #endregion

    private async void ViewRegisterAsync(ActivityIndicator activityIndicator, string page)
    {
        try
        {
            activityIndicator.IsVisible = true;
            _overallViewModel.IsLoading = true; // Ativa o indicador de carregamento
            await Navigation.PushAsync(new Registers(page, _dbService, _healthRecordService, _vaccineService, _addRegisterViewModel));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
        finally
        {
            _overallViewModel.IsLoading = false; // Desativa o indicador de carregamento
            activityIndicator.IsVisible = false;
        }
    }

    private async void Btn_Notes_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new NotesPage(_notesPageViewModel));
        }
        catch (Exception ex)
        {
            await this.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }

    private async void Btn_Menu_Clicked(object sender, EventArgs e)
    {
        string option = Convert.ToString(await this.ShowPopupAsync(new Menu()));

        if (option == "1")
        {
            await Navigation.PushAsync(new AddRegister(_dbService));
        }

        if (option == "2")
        {
            await Navigation.PushAsync(new AddHouse(_dbService));
        }

        if (option == "3")
        {
            await Navigation.PushAsync(new NotesPage(_notesPageViewModel));
        }

        if (option == "4")
        {
            await Navigation.PushAsync(new AllVisits());
        }

        if (option == "5")
        {
            var answer = await this.ShowPopupAsync(new DisplayPopUp("Sair", "Deseja desconectar-se?", true, "Sair", true, "Cancelar"));

            if (!Convert.ToBoolean(answer))
            {
                await Navigation.PopAsync();
            }
        }
    }
}