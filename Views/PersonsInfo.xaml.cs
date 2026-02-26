using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class PersonsInfo : Popup
{
    internal PersonsInfo(Patient record)
    {
        InitializeComponent();
        BindingContext = new PersonsInfoViewModel(record);
    }
}