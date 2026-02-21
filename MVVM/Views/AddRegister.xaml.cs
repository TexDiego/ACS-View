using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;

namespace ACS_View.MVVM.Views;

public partial class AddRegister : ContentPage, IQueryAttributable
{
    private readonly AddRegisterViewModel _viewModel;

    public AddRegister(IDatabaseService _db)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddRegisterViewModel(_db);
        MainThread.BeginInvokeOnMainThread(async () => await _viewModel.LoadConditions());
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("record", out var record))
            if (record is Patient p)
                await _viewModel.LoadConditions(p.Id);
    }
}