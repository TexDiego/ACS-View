using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class OverallView : ContentPage
{
    private readonly OverallViewModel _overallViewModel = new();

    public OverallView()
    {
        InitializeComponent();
        BindingContext = _overallViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_overallViewModel.LoadSummaryCommand.CanExecute(null))
            await _overallViewModel.LoadSummaryCommand.ExecuteAsync(null);
    }

    private async void Btn_OverallViewExit_Clicked(object sender, EventArgs e)
    {
        var answer = await this.ShowPopupAsync(new DisplayPopUp("Sair", "Deseja desconectar-se?", true, "Sair", true, "Cancelar"));
        if (!Convert.ToBoolean(answer))
            await Navigation.PopAsync();
    }

    private async void Btn_OverallAdd_Clicked(object sender, EventArgs e)
    {
        // Navega para AddRegister, injetando via construtor no container DI (recomendado)
        await Navigation.PushAsync(App.ServiceProvider.GetRequiredService<AddRegister>());
    }

    private async void Btn_Notes_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(App.ServiceProvider.GetRequiredService<NotesPage>());
    }

    private async void Btn_Menu_Clicked(object sender, EventArgs e)
    {
        var option = Convert.ToString(await this.ShowPopupAsync(new Menu()));

        switch (option)
        {
            case "1":
                await Navigation.PushAsync(new AddRegister());
                break;
            case "2":
                await Navigation.PushAsync(new AddHouse());
                break;
            case "3":
                await Navigation.PushAsync(new NotesPage());
                break;
            case "4":
                await Navigation.PushAsync(new AllVisits());
                break;
            case "5":
                var answer = await this.ShowPopupAsync(new DisplayPopUp("Sair", "Deseja desconectar-se?", true, "Sair", true, "Cancelar"));
                if (!Convert.ToBoolean(answer))
                    await Navigation.PopAsync();
                break;
        }
    }
}