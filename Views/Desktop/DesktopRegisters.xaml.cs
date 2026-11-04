using ACS_View.ViewModels;
namespace ACS_View.Views.Desktop;
public partial class DesktopRegisters : ContentView
{
    public DesktopRegisters() => InitializeComponent();
    public void ScrollToPatient(object patient) => collectionView.ScrollTo(patient, position: ScrollToPosition.Center, animate: true);
    private void SB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is RegistersViewModel vm) vm.SearchPatientsCommand.Execute(e.NewTextValue);
    }
    private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e) => ScrollToTopButtonController.UpdateVisibility(BackToTopButton, e);
    private void BackToTopButton_Clicked(object sender, EventArgs e) => ScrollToTopButtonController.ScrollToTop(collectionView, BackToTopButton);
}
