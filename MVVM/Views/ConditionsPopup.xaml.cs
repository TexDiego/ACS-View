using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class ConditionsPopup : Popup
{
	internal ConditionsPopup(ConditionCategoryVM condition)
	{
		InitializeComponent();
		BindingContext = new ConditionPopupViewModel(condition);
    }
}