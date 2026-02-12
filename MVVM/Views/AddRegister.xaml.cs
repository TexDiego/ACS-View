using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class AddRegister : ContentPage
{
    private readonly AddRegisterViewModel _viewModel = new();

    public AddRegister()
    {
        InitializeComponent();
        BindingContext = _viewModel;

        Entry_Birth.MinimumDate = DateTime.Today.AddYears(-120);
        Entry_Birth.MaximumDate = DateTime.Today;
        Entry_Birth.Date = DateTime.Today;
    }

    public AddRegister(HealthRecord record) : this()
    {
        _viewModel.SetRecord(record);
    }
}