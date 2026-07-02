using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class DialogPopup : Popup<object>
{
    public DialogPopup(DialogPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        if (viewModel.IsTextPrompt)
        {
            Loaded += (_, _) => PromptEntry.Focus();
        }
    }

    private DialogPopupViewModel ViewModel => (DialogPopupViewModel)BindingContext;

    private async void PrimaryButton_Clicked(object sender, EventArgs e)
    {
        var result = ViewModel.IsTextPrompt
            ? ViewModel.InputText
            : ViewModel.PrimaryResult;

        await CloseAsync(result);
    }

    private async void SecondaryButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(ViewModel.SecondaryResult);
    }

    private async void OptionButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: DialogOption option })
        {
            await CloseAsync(option.Value);
        }
    }
}
