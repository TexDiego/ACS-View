using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class HousesPage : ContentPage, IQueryAttributable
{
    private readonly HousesPageViewModel _viewModel;
    private string? _lastMetricNavigationId;
    private bool _hasAppeared;

    public HousesPage(HousesPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.LoadHousesCommand.Execute(e.NewTextValue);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("filterKey", out var filterKey))
        {
            return;
        }

        var incomingNavigationId = query.TryGetValue("metricNavigationId", out var navigationId)
            ? navigationId?.ToString()
            : null;

        if (!string.IsNullOrWhiteSpace(incomingNavigationId) &&
            string.Equals(_lastMetricNavigationId, incomingNavigationId, StringComparison.Ordinal))
        {
            return;
        }

        _lastMetricNavigationId = incomingNavigationId;
        _viewModel.SetFilter(filterKey?.ToString());

        if (_hasAppeared)
        {
            _ = _viewModel.LoadInitialHousesAsync();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasAppeared = true;
        _ = _viewModel.LoadInitialHousesAsync();
    }
}
