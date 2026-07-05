using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class VaccineApplicationPopup : Popup<VaccineApplicationRequestDto>
{
    private readonly VaccineApplicationPopupViewModel viewModel;

    public VaccineApplicationPopup(int patientId, PatientVaccineDoseDto dose)
    {
        InitializeComponent();
        BindingContext = viewModel = new VaccineApplicationPopupViewModel(patientId, dose);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (!viewModel.TryCreateRequest(out var request) || request is null)
        {
            return;
        }

        await CloseAsync(request);
    }
}
