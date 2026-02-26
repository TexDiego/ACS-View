using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using System.Text.RegularExpressions;

namespace ACS_View.Views;

public partial class AddHouse : ContentPage, IQueryAttributable
{
    private readonly AddHouseViewModel viewModel = new();
    private readonly Regex regex = GetDigitsOnlyRegex();

    public AddHouse()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("house", out var house))
            viewModel.HouseModel = (House)house;
    }

    private void Entry_CEP_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.NewTextValue) && !regex.IsMatch(e.NewTextValue))
            viewModel.HouseModel.CEP = e.OldTextValue;
    }

    [GeneratedRegex("^[0-9]*$")]
    private static partial Regex GetDigitsOnlyRegex();
}