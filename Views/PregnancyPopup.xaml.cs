using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class PregnancyPopup : Popup<PatientPregnancy>
{
    private PregnancyPopupViewModel ViewModel => (PregnancyPopupViewModel)BindingContext;

    public PregnancyPopup(PregnancyDetailsDto details)
    {
        InitializeComponent();
        BindingContext = new PregnancyPopupViewModel(details);
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(ViewModel.ToPregnancy());
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void TopOverlay_Tapped(object sender, TappedEventArgs e)
    {
        await CloseAsync();
    }
}
