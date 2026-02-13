using ACS_View.MVVM.Models;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class AddRegister : ContentPage, IQueryAttributable
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("record", out var record))
            _viewModel.SetRecord((HealthRecord)record);
    }
}