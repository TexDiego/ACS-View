using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class FilterPopup : Popup
{
	public FilterPopup()
	{
		InitializeComponent();
		BindingContext = new FilterPopupViewModel();
	}
}