using ACS_View.ViewModels;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;

namespace ACS_View.Views;

public partial class VetorialControlOptions : ContentPage
{
	public VetorialControlOptions(VetorialControlOptionsViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}

    private void OnResultsHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (ResidenceResults.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recycler)
        {
            // A search field in the recycled header can keep native focus even with
            // the keyboard hidden. Recovering that focus after layout scrolls to it.
            recycler.PreserveFocusAfterLayout = false;
        }
#endif
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        EndSearchEditing();
        await ((VetorialControlOptionsViewModel)BindingContext).SearchCommand.ExecuteAsync(null);
    }

    private void OnSelectionChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(VetorialControlOptionsViewModel.SelectedCount))
            EndSearchEditing();
    }

    private void EndSearchEditing()
    {
        StreetEntry.Unfocus();
        StartNumberEntry.Unfocus();
        EndNumberEntry.Unfocus();
#if ANDROID
        // Give focus to the list itself so Android cannot choose a header input
        // when a checkbox/count update or keyboard dismissal triggers layout.
        if (ResidenceResults.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recycler)
        {
            recycler.FocusableInTouchMode = true;
            var focusability = recycler.DescendantFocusability;
            try
            {
                recycler.DescendantFocusability = Android.Views.DescendantFocusability.BeforeDescendants;
                recycler.RequestFocus();
            }
            finally
            {
                recycler.DescendantFocusability = focusability;
            }
            if (recycler.Context?.GetSystemService(Android.Content.Context.InputMethodService)
                is Android.Views.InputMethods.InputMethodManager keyboard)
                keyboard.HideSoftInputFromWindow(recycler.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
        }
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        var viewModel = (VetorialControlOptionsViewModel)BindingContext;
        viewModel.PropertyChanging -= OnSelectionChanging;
        viewModel.PropertyChanging += OnSelectionChanging;
        viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        ((VetorialControlOptionsViewModel)BindingContext).PropertyChanging -= OnSelectionChanging;
        base.OnDisappearing();
    }
}
