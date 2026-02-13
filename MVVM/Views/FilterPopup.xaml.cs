using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class FilterPopup : Popup
{
	public FilterPopup()
	{
		InitializeComponent();
		BindingContext = new FilterPopupViewModel();
	}
}