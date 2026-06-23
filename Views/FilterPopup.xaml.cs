using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class FilterPopup : Popup<PatientListFilterDto>
{
    private readonly string _filterKey;
    private readonly FilterPopupViewModel _viewModel;

    public FilterPopup()
        : this(new PatientListFilterDto())
    {
    }

    public FilterPopup(PatientListFilterDto filter)
    {
        InitializeComponent();

        _filterKey = filter.FilterKey;
        BindingContext = _viewModel = new FilterPopupViewModel(filter);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void ApplyButton_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryCreateFilter(_filterKey, out var filter, out var errorMessage))
        {
            await Shell.Current.DisplayAlertAsync("Filtro inválido", errorMessage, "OK");
            return;
        }

        await CloseAsync(filter);
    }
}
