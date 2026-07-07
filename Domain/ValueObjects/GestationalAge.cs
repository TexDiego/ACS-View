namespace ACS_View.Domain.ValueObjects;

public readonly record struct GestationalAge(int Weeks, int Days)
{
    public int TotalDays => (Weeks * 7) + Days;

    public override string ToString()
    {
        var weekText = Weeks == 1 ? "1 semana" : $"{Weeks} semanas";
        if (Days <= 0)
        {
            return weekText;
        }

        var dayText = Days == 1 ? "1 dia" : $"{Days} dias";
        return $"{weekText} e {dayText}";
    }
}
