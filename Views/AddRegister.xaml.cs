using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class AddRegister : ContentPage, IQueryAttributable
{
    private readonly AddRegisterViewModel _viewModel;

    public AddRegister(IUserDialogService dialogue, IPatientService patientService)
    {
        InitializeComponent();
        BindingContext = _viewModel = new AddRegisterViewModel(dialogue, patientService);
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("record", out var record))
            if (record is Patient p)
                await _viewModel.LoadPatiant(p.Id);
    }
}