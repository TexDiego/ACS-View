using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel model;
    public BackupPage(BackupViewModel model)
    {
        InitializeComponent();this.model=model;BindingContext=model;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();await model.LoadAsync();
    }
    protected override void OnDisappearing()
    {
        model.Leave();base.OnDisappearing();
    }
    protected override bool OnBackButtonPressed()=>model.IsBusy || model.RestartRequired || base.OnBackButtonPressed();
}
