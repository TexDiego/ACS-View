using ACS_View.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.ViewModels;

public partial class HouseFilterPopupViewModel : BaseViewModel
{
    [ObservableProperty] private string startNumber;
    [ObservableProperty] private string endNumber;
    [ObservableProperty] private int parityIndex;
    [ObservableProperty] private string errorMessage = string.Empty;
    public string[] ParityOptions { get; } = ["Todos", "Pares", "Ímpares"];

    public HouseFilterPopupViewModel(HouseNumberFilterDto filter)
    {
        startNumber = filter.Start?.ToString() ?? string.Empty;
        endNumber = filter.End?.ToString() ?? string.Empty;
        parityIndex = (int)filter.Parity;
    }

    public bool TryCreateFilter(out HouseNumberFilterDto? filter)
    {
        try
        {
            filter = HouseNumberFilterDto.Parse(StartNumber, EndNumber, ParityIndex);
            ErrorMessage = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            filter = null;
            ErrorMessage = ex.Message;
            return false;
        }
    }
}
