using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class PersonsInfo : Popup
{
    private readonly HouseService _houseService;
    private readonly PersonsInfoViewModel _personsInfoViewModel;
    private readonly string susNumber;


    public PersonsInfo(HealthRecord record, DatabaseService databaseService)
    {
        InitializeComponent();

        _houseService = new HouseService(databaseService);
        _personsInfoViewModel = new PersonsInfoViewModel(record);
        susNumber = record.SusNumber;

        int width = (int)Application.Current.MainPage.Width;
        PopupContent.WidthRequest = width - 30;
        MothersName.MaximumWidthRequest = width - LabelMother.Width - 80;

        BindingContext = _personsInfoViewModel;
    }

    public async Task LoadAddressAsync()
    {
        try
        {
            var house = await _houseService.GetHouseBySusAsync(susNumber);

            if (house != null)
            {
                var rua = house.Rua ?? "";
                var numeroRua = house.NumeroCasa ?? "";
                var complemento = house.Complemento;

                Lbl_endereco.Text = $"{rua}, {numeroRua}";
                Lbl_complemento.IsVisible = house.PossuiComplemento;
                Lbl_complemento.Text = house.PossuiComplemento ? house.Complemento : "Sem complemento";
            }
            else
            {
                Lbl_endereco.Text = "Endere�o n�o encontrado";
                Lbl_complemento.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}