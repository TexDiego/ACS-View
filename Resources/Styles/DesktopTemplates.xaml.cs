using ACS_View.Application.DTOs;
using ACS_View.ViewModels;

namespace ACS_View.Resources.Styles;

public partial class DesktopTemplates : ResourceDictionary
{
    public DesktopTemplates() => InitializeComponent();

    private void PatientActions_Clicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton { BindingContext: PatientListItemDto patient } button) return;
        Element? parent = button;
        while (parent is not null && parent is not ContentPage) parent = parent.Parent;
        if (parent?.BindingContext is not RegistersViewModel vm) return;
        var menu = new MenuFlyout
        {
            new MenuFlyoutItem { Text = "Editar paciente", Command = vm.EditCommand, CommandParameter = patient },
            new MenuFlyoutSeparator(),
            new MenuFlyoutItem { Text = "Excluir paciente", Command = vm.DeleteCommand, CommandParameter = patient.Id }
        };
        FlyoutBase.SetContextFlyout(button, menu);
#if WINDOWS
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement native)
            native.ContextFlyout?.ShowAt(native);
#endif
    }
}
