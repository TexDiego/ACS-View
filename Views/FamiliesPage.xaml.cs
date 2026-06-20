using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class FamiliesPage : ContentPage, IQueryAttributable
{
    private FamiliesViewModel? _viewModel;
    private bool _hasAppeared;

    public FamiliesPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
        {
            int parsedId = (int)id;
            BindingContext = _viewModel = new FamiliesViewModel(parsedId);

            if (_hasAppeared)
            {
                _ = _viewModel.LoadFamiliesAsync();
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasAppeared = true;

        if (_viewModel != null)
        {
            if (_viewModel.ShouldSkipTransientReload())
            {
                return;
            }

            _ = _viewModel.LoadFamiliesAsync();
        }
    }
}
