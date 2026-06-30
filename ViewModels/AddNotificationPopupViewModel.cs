using ACS_View.Application.DTOs;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class AddNotificationPopupViewModel : BaseViewModel
{
    public const int MaxMessageLength = 160;
    private DateTime minimumDate;
    private DateTime selectedDate;
    private TimeSpan selectedTime;
    private string message;
    private string characterCountText;
    private string errorMessage = string.Empty;
    private bool hasError;

    public AddNotificationPopupViewModel(string noteContent)
    {
        var initialDate = DateTime.Now.AddDays(1);

        minimumDate = DateTime.Today;
        selectedDate = initialDate.Date;
        selectedTime = initialDate.TimeOfDay;
        message = TrimMessage(noteContent);
        characterCountText = BuildCharacterCountText(message);

        Suggestions =
        [
            new NotificationSuggestionOption(1, DateTime.Now.AddDays(1)),
            new NotificationSuggestionOption(3, DateTime.Now.AddDays(3)),
            new NotificationSuggestionOption(7, DateTime.Now.AddDays(7))
        ];

        SelectSuggestionCommand = new Command<NotificationSuggestionOption>(SelectSuggestion);
        SelectSuggestion(Suggestions[0]);
    }

    public double Width => Math.Min(
        (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 24,
        380);

    public int MessageMaxLength => MaxMessageLength;
    public ObservableCollection<NotificationSuggestionOption> Suggestions { get; }
    public ICommand SelectSuggestionCommand { get; }

    public DateTime MinimumDate
    {
        get => minimumDate;
        set => SetProperty(ref minimumDate, value);
    }

    public DateTime SelectedDate
    {
        get => selectedDate;
        set
        {
            if (SetProperty(ref selectedDate, value))
            {
                ClearSelectedSuggestion();
            }
        }
    }

    public TimeSpan SelectedTime
    {
        get => selectedTime;
        set
        {
            if (SetProperty(ref selectedTime, value))
            {
                ClearSelectedSuggestion();
            }
        }
    }

    public string Message
    {
        get => message;
        set
        {
            if (!SetProperty(ref message, value))
            {
                return;
            }

            CharacterCountText = BuildCharacterCountText(value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                ClearError();
            }
        }
    }

    public string CharacterCountText
    {
        get => characterCountText;
        private set => SetProperty(ref characterCountText, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public bool HasError
    {
        get => hasError;
        private set => SetProperty(ref hasError, value);
    }

    public bool TryCreateRequest(out NoteNotificationRequestDto? request)
    {
        request = null;

        if (string.IsNullOrWhiteSpace(Message))
        {
            SetError("Informe uma mensagem para a notificação.");
            return false;
        }

        var notifyOn = SelectedDate.Date.Add(SelectedTime);
        if (notifyOn <= DateTime.Now)
        {
            SetError("Escolha uma data e horário futuros.");
            return false;
        }

        request = new NoteNotificationRequestDto(notifyOn, Message.Trim());
        ClearError();
        return true;
    }

    private void SelectSuggestion(NotificationSuggestionOption? option)
    {
        if (option is null)
        {
            return;
        }

        foreach (var suggestion in Suggestions)
        {
            suggestion.IsSelected = suggestion == option;
        }

        SelectedDate = option.NotifyOn.Date;
        SelectedTime = option.NotifyOn.TimeOfDay;
        ClearError();
    }

    private void ClearSelectedSuggestion()
    {
        var selectedNotifyOn = SelectedDate.Date.Add(SelectedTime);

        foreach (var suggestion in Suggestions)
        {
            suggestion.IsSelected = Math.Abs((suggestion.NotifyOn - selectedNotifyOn).TotalSeconds) < 1;
        }

        ClearError();
    }

    private static string TrimMessage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= MaxMessageLength
            ? trimmed
            : trimmed[..MaxMessageLength];
    }

    private static string BuildCharacterCountText(string value)
    {
        return $"{value?.Length ?? 0}/{MaxMessageLength} caracteres";
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
