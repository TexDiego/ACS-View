using ACS_View.MVVM.Models;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class PersonsInfo : Popup
{
    internal PersonsInfo(Patient record)
    {
        InitializeComponent();
        BindingContext = new PersonsInfoViewModel(record);
    }
}