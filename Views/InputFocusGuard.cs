using Microsoft.Maui.ApplicationModel;

namespace ACS_View.Views;

internal static class InputFocusGuard
{
    public static void ClearTextInputFocus(Element? root)
    {
        if (root is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UnfocusTextInputs(root);
            _ = ClearTextInputFocusAfterNativeRestoreAsync(root);
        });
    }

    private static async Task ClearTextInputFocusAfterNativeRestoreAsync(Element root)
    {
        await Task.Delay(75);

        if (root.Handler is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => UnfocusTextInputs(root));
    }

    private static void UnfocusTextInputs(Element element)
    {
        if (element is Entry or Editor or SearchBar)
        {
            ((VisualElement)element).Unfocus();
        }

#pragma warning disable CS0618
        if (element is IElementController controller)
        {
            foreach (var child in controller.LogicalChildren)
            {
                UnfocusTextInputs(child);
            }
        }
#pragma warning restore CS0618
    }
}
