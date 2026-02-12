using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class PersonsInfo : Popup
{
    private readonly PersonsInfoViewModel _viewModel;

    public PersonsInfo(HealthRecord record)
    {
        InitializeComponent();

        _viewModel = new PersonsInfoViewModel(record);
        BindingContext = _viewModel;

        int width = (int)Application.Current.MainPage.Width;
        PopupContent.WidthRequest = width - 30;
        MothersName.MaximumWidthRequest = width - LabelMother.Width - 80;
    }

    public async Task LoadAddressAsync()
    {
        try
        {
            await _viewModel.CarregarEnderecoAsync();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}
