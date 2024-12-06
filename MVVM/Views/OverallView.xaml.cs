using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModel;

namespace ACS_View.MVVM.Views;

public partial class OverallView : ContentPage
{
    private readonly DatabaseService _dbService;
    private OverallViewModel _overallViewModel;

    public OverallView(DatabaseService dbService, HealthRecordService healthRecordService, OverallViewModel overallViewModel)
    {
        InitializeComponent();
        _dbService = dbService;
        _overallViewModel = overallViewModel;
        _ = InitializeAsync(); // Evita travamento por não bloquear a UI
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _overallViewModel;
        _overallViewModel.AtualizarContagens();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _dbService.InitializeDatabaseAsync(); // Inicialize o banco de dados sem bloquear
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao inicializar o banco de dados: {ex.Message}", "OK");
        }
    }

    private async void Btn_OverallViewExit_Clicked(object sender, EventArgs e)
    {
		if (await DisplayAlert("Sair", "Deseja desconectar-se?", "Sim", "Cancelar"))
			await Navigation.PopAsync();
    }

    private async void Btn_OverallAdd_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddRegister());
    }

    private void Btn_Gestantes_Clicked(object sender, EventArgs e)
    {
        ViewRegisterAsync(AIGestantes, "GESTANTE");
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

    private void Btn_Menor2Anos_Clicked(object sender, EventArgs e)
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

    private async void ViewRegisterAsync(ActivityIndicator activityIndicator, string page)
    {
        try
        {
            activityIndicator.IsVisible = true;
            _overallViewModel.IsLoading = true; // Ativa o indicador de carregamento
            await Navigation.PushAsync(new Registers(page));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "Voltar");
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
            await Navigation.PushAsync(new NotesPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "Voltar");
        }
    }
}