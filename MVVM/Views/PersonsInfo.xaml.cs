using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace ACS_View.MVVM.Views;

public partial class PersonsInfo : Popup
{
    private readonly HouseService _houseService;
    private readonly string susNumber;

    public HealthRecord PersonInfo { get; set; }
    public ICommand ShowPersonInfo { get; }

    public PersonsInfo(HealthRecord record, DatabaseService databaseService)
    {
        InitializeComponent();

        _houseService = new HouseService(databaseService);
        susNumber = record.SusNumber;

        int width = (int)Application.Current.MainPage.Width;
        PopupContent.WidthRequest = width - 30;

        PersonInfo = record;
        BindingContext = this;
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
                Lbl_endereco.Text = "Endereço não encontrado";
                Lbl_complemento.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.ShowPopupAsync(new DisplayPopUp("Erro", ex.Message, true, "Voltar", false, ""));
        }
    }
}