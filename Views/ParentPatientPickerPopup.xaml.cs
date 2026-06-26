using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class ParentPatientPickerPopup : Popup<PatientListItemDto>
{
    private readonly ParentPatientPickerViewModel _viewModel;

    internal ParentPatientPickerPopup(IPatientService patientService, int? excludedPatientId, string title)
    {
        InitializeComponent();
        BindingContext = _viewModel = new ParentPatientPickerViewModel(patientService, excludedPatientId, title);
        _ = _viewModel.LoadAsync();
    }

    private async void OnPatientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PatientListItemDto selectedPatient)
        {
            return;
        }

        await CloseAsync(selectedPatient);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }
}
