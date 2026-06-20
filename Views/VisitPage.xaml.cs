using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class VisitPage : Popup<Visits>
{
    private readonly IHouseService _houseService = App.StaticServiceProvider.GetRequiredService<IHouseService>();
    private readonly int _houseId;
    private readonly int _familyId;

    public VisitPage(int houseId, int familyId)
    {
        InitializeComponent();

        _houseId = houseId;
        _familyId = familyId;

        layoutGrid.WidthRequest = (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 50;
        UpdateOutcomeColors();
    }

    private async void AddVisitButton_Clicked(object sender, EventArgs e)
    {
        var descricaoSelecionada = GetSelectedOutcome();

        if (string.IsNullOrWhiteSpace(descricaoSelecionada))
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Selecione o desfecho da visita.", "OK");
            return;
        }

        var visit = new Visits
        {
            HouseId = _houseId,
            FamilyId = _familyId,
            Date = DateTime.Now,
            Description = descricaoSelecionada,
            Address = await GetAddress()
        };

        await CloseAsync(visit);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private string? GetSelectedOutcome()
    {
        return Descricao.Children
            .OfType<RadioButton>()
            .FirstOrDefault(rb => rb.IsChecked)
            ?.Content
            ?.ToString()
            ?.Trim();
    }

    private async Task<string> GetAddress()
    {
        try
        {
            var house = await _houseService.GetHouseByIdAsync(_houseId);

            if (house is null)
            {
                return "Endereço não encontrado.";
            }

            var street = string.IsNullOrWhiteSpace(house.Rua) ? "Endereço sem rua" : house.Rua.Trim();
            var number = string.IsNullOrWhiteSpace(house.NumeroCasa) ? "S/N" : house.NumeroCasa.Trim();

            return house.PossuiComplemento && !string.IsNullOrWhiteSpace(house.Complemento)
                ? $"{street}, {number} - {house.Complemento.Trim()}"
                : $"{street}, {number}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter endereço: {ex.Message}");
            return "Erro ao obter endereço.";
        }
    }

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton && e.Value)
        {
            UpdateOutcomeColors();
        }
    }

    private void UpdateOutcomeColors()
    {
        foreach (var child in Descricao.Children)
        {
            if (child is RadioButton rb)
            {
                rb.TextColor = rb.IsChecked
                    ? GetCorPorDescricao(rb.Content?.ToString()?.Trim())
                    : ThemeColors.TextPrimary;
            }
        }
    }

    private static Color GetCorPorDescricao(string? descricao) => descricao switch
    {
        "Realizada" => ThemeColors.Success,
        "Ausente" => ThemeColors.Warning,
        "Recusada" => ThemeColors.Danger,
        _ => ThemeColors.TextPrimary
    };
}
