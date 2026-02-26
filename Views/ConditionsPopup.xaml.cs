using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class ConditionsPopup : Popup
{
	internal ConditionsPopup(ConditionCategoryVM condition)
	{
		InitializeComponent();
		BindingContext = new ConditionPopupViewModel(condition);
    }
}