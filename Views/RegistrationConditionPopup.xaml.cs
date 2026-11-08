using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class RegistrationConditionPopup : Popup<bool>
{
    internal RegistrationConditionPopup(AddRegisterViewModel parent, HealthConditions condition)
    {
        InitializeComponent();
        BindingContext = new { Parent = parent, Condition = condition };
        var display = DeviceDisplay.Current.MainDisplayInfo;
        WidthRequest = Math.Min(560, display.Width / display.Density - 48);
        HeightRequest = Math.Min(650, display.Height / display.Density - 120);
        Content.HeightRequest = HeightRequest;
    }

    private async void Done_Clicked(object sender, EventArgs e) => await CloseAsync(true);
}
