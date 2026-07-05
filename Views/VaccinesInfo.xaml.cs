using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class VaccinesInfo : Popup
{
    public VaccinesInfo(PatientVaccineDoseDto dose)
    {
        InitializeComponent();
        BindingContext = new VaccinesInfoViewModel(dose);
    }

    public VaccinesInfo(string doseKey)
    {
        InitializeComponent();
        BindingContext = new VaccinesInfoViewModel(doseKey);
    }

    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }
}
