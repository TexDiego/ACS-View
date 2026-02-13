using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class FilterPopupViewModel : BaseViewModel
    {
        public double MaxScreenWidth => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;

        [ObservableProperty] private int filterBy = 0;
        [ObservableProperty] private int orderBy = 0;

    }
}