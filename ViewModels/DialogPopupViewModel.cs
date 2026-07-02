using System.Collections.ObjectModel;

namespace ACS_View.ViewModels;

public sealed class DialogPopupViewModel : BaseViewModel
{
    private DialogPopupViewModel(
        string title,
        string message,
        string primaryText,
        object? primaryResult,
        string? secondaryText = null,
        object? secondaryResult = null)
    {
        Title = title;
        Message = message;
        PrimaryText = primaryText;
        PrimaryResult = primaryResult;
        SecondaryText = secondaryText ?? string.Empty;
        SecondaryResult = secondaryResult;
    }

    public string Title { get; }
    public string Message { get; }
    public string PrimaryText { get; }
    public object? PrimaryResult { get; }
    public string SecondaryText { get; }
    public object? SecondaryResult { get; }
    public string Placeholder { get; private init; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public int MaxLength { get; private init; } = int.MaxValue;
    public Keyboard Keyboard { get; private init; } = Keyboard.Text;
    public ObservableCollection<DialogOption> Options { get; } = [];

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasSecondaryAction => !string.IsNullOrWhiteSpace(SecondaryText);
    public bool HasPrimaryAction => !string.IsNullOrWhiteSpace(PrimaryText);
    public bool HasOptions => Options.Count > 0;
    public bool IsPrompt => false;
    public bool IsTextPrompt { get; private init; }

    public static DialogPopupViewModel Alert(string title, string message, string cancel)
    {
        return new DialogPopupViewModel(title, message, cancel, true);
    }

    public static DialogPopupViewModel Confirmation(string title, string message, string accept, string cancel)
    {
        return new DialogPopupViewModel(title, message, accept, true, cancel, false);
    }

    public static DialogPopupViewModel Prompt(
        string title,
        string message,
        string accept,
        string cancel,
        string? placeholder,
        int maxLength,
        Keyboard? keyboard,
        string initialValue)
    {
        return new DialogPopupViewModel(title, message, accept, null, cancel, null)
        {
            IsTextPrompt = true,
            Placeholder = placeholder ?? string.Empty,
            MaxLength = maxLength > 0 ? maxLength : int.MaxValue,
            Keyboard = keyboard ?? Keyboard.Text,
            InputText = initialValue
        };
    }

    public static DialogPopupViewModel ActionSheet(
        string title,
        string cancel,
        string? destruction,
        IReadOnlyList<string> buttons)
    {
        var viewModel = new DialogPopupViewModel(title, string.Empty, string.Empty, null, cancel, cancel);

        if (!string.IsNullOrWhiteSpace(destruction))
        {
            viewModel.Options.Add(new DialogOption(destruction, destruction, true));
        }

        foreach (var button in buttons.Where(button => !string.IsNullOrWhiteSpace(button)))
        {
            viewModel.Options.Add(new DialogOption(button, button));
        }

        return viewModel;
    }
}

public sealed class DialogOption(string text, string value, bool isDestructive = false)
{
    public string Text { get; } = text;
    public string Value { get; } = value;
    public bool IsDestructive { get; } = isDestructive;
}
