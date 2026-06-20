using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using System.Text.RegularExpressions;

namespace ACS_View.Views;

public partial class AddHouse : ContentPage, IQueryAttributable
{
    private readonly AddHouseViewModel viewModel;
    private readonly Regex regex = GetDigitsOnlyRegex();
    private int? _houseId;
    private bool _loaded;

    public AddHouse(AddHouseViewModel vm)
    {
        InitializeComponent();
        BindingContext = viewModel = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("house", out var house))
        {
            viewModel.HouseModel = (House)house;
            _loaded = true;
        }

        if (query.TryGetValue("houseId", out var houseId))
        {
            _houseId = Convert.ToInt32(houseId);
            _loaded = false;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_loaded && _houseId is int houseId)
        {
            _loaded = true;
            _ = viewModel.LoadHouseAsync(houseId);
        }
    }

    private void Entry_CEP_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.NewTextValue) && !regex.IsMatch(e.NewTextValue))
            viewModel.HouseModel.CEP = e.OldTextValue;
    }

    [GeneratedRegex("^[0-9]*$")]
    private static partial Regex GetDigitsOnlyRegex();
}
