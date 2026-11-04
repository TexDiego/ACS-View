using ACS_View.ViewModels;
namespace ACS_View.Views.Desktop;
public partial class DesktopAddRegister : ContentView
{
    private AddRegisterViewModel ViewModel => (AddRegisterViewModel)BindingContext;
    public DesktopAddRegister() => InitializeComponent();
    private bool? _sexOptionsHorizontal;

    private void OnEditPhoneClicked(object? sender, EventArgs e) => PhoneEntry.Focus();

    private void OnEditSusClicked(object? sender, EventArgs e) => SusEntry.Focus();

    private void OnPrimarySusCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton { BindingContext: ACS_View.Domain.ValueObjects.SusNumber number }
            && ViewModel.SusEditor.Numbers.Contains(number))
            ViewModel.SusEditor.PrimaryNumber = number;
    }

    private void OnPrimaryPhoneCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton { BindingContext: ACS_View.Domain.ValueObjects.Phone phone }
            && ViewModel.PhoneEditor.Phones.Contains(phone))
            ViewModel.PhoneEditor.PrimaryPhone = phone;
    }

    private void OnSexOptionsSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid grid || grid.Width <= 0)
        {
            return;
        }

        // Three compact choices on typical screens; two rows at narrow widths.
        var horizontal = grid.Width >= 300;
        if (_sexOptionsHorizontal == horizontal) return;
        _sexOptionsHorizontal = horizontal;
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        if (!horizontal) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var index = 0; index < (horizontal ? 3 : 2); index++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var index = 0; index < grid.Children.Count; index++)
        {
            grid.SetRow(grid.Children[index], !horizontal && index == 2 ? 1 : 0);
            grid.SetColumn(grid.Children[index], !horizontal && index == 2 ? 0 : index);
            grid.SetColumnSpan(grid.Children[index], !horizontal && index == 2 ? 2 : 1);
        }
    }

}
