using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class PersonsInfo : Popup
{
    public PersonsInfo(PersonsInfoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public void SetPatient(Patient patient)
    {
        if (BindingContext is PersonsInfoViewModel viewModel)
            viewModel.SetPatient(patient);
    }
}