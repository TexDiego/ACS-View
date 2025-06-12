using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class FamiliesPage : ContentPage
{
    private FamiliesViewModel _viewModel;
    private DatabaseService _databaseService = new();
    private FamilyService FamilyService;

    public FamiliesPage(FamiliesViewModel viewModel)
	{
        InitializeComponent();
        FamilyService = new FamilyService(_databaseService);
        _viewModel = viewModel;
        BindingContext = viewModel;
        Console.WriteLine($"Max Family ID: {FamilyService.GetMaxIdAsync(_viewModel._idHouse).ToString()}");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadFamilies();
    }

    private async void Btn_Voltar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}