using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;

namespace ACS_View.Resources.Styles;

public partial class DesktopFamilies : ResourceDictionary
{
    public DesktopFamilies() => InitializeComponent();

    private void FamilyActions_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: Familia family } button) return;
        Element? parent = button;
        while (parent is not null && parent is not ContentPage) parent = parent.Parent;
        if (parent?.BindingContext is not FamiliesViewModel vm) return;
        var menu = new MenuFlyout
        {
            new MenuFlyoutItem { Text = "Registrar visita", Command = vm.VisitFamilyCommand, CommandParameter = family.IdFamily },
            new MenuFlyoutItem { Text = "Editar família", Command = vm.EditFamilyCommand, CommandParameter = family.IdFamily },
            new MenuFlyoutSeparator(),
            new MenuFlyoutItem { Text = "Excluir família", Command = vm.DeleteFamilyCommand, CommandParameter = family.IdFamily }
        };
        FlyoutBase.SetContextFlyout(button, menu);
#if WINDOWS
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement native)
            native.ContextFlyout?.ShowAt(native);
#endif
    }
}
