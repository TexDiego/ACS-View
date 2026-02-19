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
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("record", out var record))
            if (record is Patient p)
                await _viewModel.LoadConditions(p.Id);
    }
}