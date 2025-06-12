using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VisitPage : Popup
{
    private readonly DatabaseService _databaseService;
    private VisitsViewModel _viewModel;
    private HouseService _houseService;

    private readonly int _houseID;
    private readonly int _familyID;

    public VisitPage(int HouseID, int FamilyID)
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _viewModel = new VisitsViewModel(_databaseService);
        _houseService = new HouseService(_databaseService);

        BindingContext = _viewModel;

        _houseID = HouseID;
        _familyID = FamilyID;

        Console.WriteLine($"Página de visitas acessado: HouseID: {_houseID}; FamilyID: {_familyID}");
    }

    private async void AddVisitButton_Clicked(object sender, EventArgs e)
    {
        string address = await GetAddress();

        var visit = new Visits
        {
            HouseId = _houseID,
            FamilyId = _familyID,
            Date = DateTime.Now,
            Description = Observations?.Text ?? string.Empty,
            Address = address
        };

        this.Close(visit);
    }

    private async Task<String> GetAddress()
    {
        try
        {
            var house = await _houseService.GetHousesById(_houseID);

            if (house != null)
            {
                return house.PossuiComplemento
                    ? $"{house.Rua}, {house.NumeroCasa} - {house.Complemento}"
                    : $"{house.Rua}, {house.NumeroCasa}";
            }
            else
            {
                return "Endereço não encontrado.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter endereço: {ex.Message}");
            return "Erro ao obter endereço.";
        }
    }
}