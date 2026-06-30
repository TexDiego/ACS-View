using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.ViewModels;

public partial class NotificationSuggestionOption : ObservableObject
{
    private bool isSelected;

    public NotificationSuggestionOption(int daysAhead, DateTime notifyOn)
    {
        DaysAhead = daysAhead;
        NotifyOn = notifyOn;
        Title = daysAhead == 1 ? "1 dia" : $"{daysAhead} dias";
        Subtitle = notifyOn.ToString("dd/MM HH:mm");
    }

    public int DaysAhead { get; }
    public DateTime NotifyOn { get; }
    public string Title { get; }
    public string Subtitle { get; }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
