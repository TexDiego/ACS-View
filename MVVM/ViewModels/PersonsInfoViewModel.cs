using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ACS_View.MVVM.ViewModels;

internal partial class PersonsInfoViewModel : BaseViewModel
{
    private readonly IPersonsInfoService _infoService = App.ServiceProvider.GetRequiredService<IPersonsInfoService>();
    private readonly IDatabaseService _db = App.ServiceProvider.GetRequiredService<IDatabaseService>();

    public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;

    [ObservableProperty] private ObservableCollection<HealthIcon> icons = [];
    [ObservableProperty] private Patient personInfo;

    [ObservableProperty] private bool iconsVisibility = false;

    [ObservableProperty] private string endereco = "Sem endereço";
    [ObservableProperty] private string complemento = string.Empty;
       

    public PersonsInfoViewModel(Patient record)
    {
        PersonInfo = record;

        MainThread.BeginInvokeOnMainThread(async () => await CarregarEnderecoAsync());
        MainThread.BeginInvokeOnMainThread(async () => await SetHealthIcons());
    }

    public async Task CarregarEnderecoAsync()
    {
        Endereco = await _infoService.GetEnderecoAsync(PersonInfo.Id);
        Complemento = await _infoService.GetComplementoAsync(PersonInfo.Id);
    }

    private async Task SetHealthIcons()
    {
        try
        {
            List<Models.HealthConditions.Condition> conditions = await GetConditionsByPatient(PersonInfo.Id);

            if (conditions is null) return;

            foreach (Models.HealthConditions.Condition condition in conditions)
            {
                if (condition.Name == "Diabetes Tipo 1" || condition.Name == "Diabetes Tipo 2" || condition.Name == "Pré-Diabetes")
                {
                    Icons.Add(new HealthIcon
                    {
                        IconSource = "diabetes",
                        Description = condition.Name
                    });
                    continue;
                }

                if (condition.Name == "Bronquite Crônica" || condition.Name == "Enfisema Pulmonar")
                {
                    Icons.Add(new HealthIcon
                    {
                        IconSource = "respiratorias",
                        Description = condition.Name
                    });
                    continue;
                }

                Debug.WriteLine(RemoverAcentos(condition.Name.ToLowerInvariant()));

                Icons.Add(new HealthIcon
                {
                    IconSource = RemoverAcentos(condition.Name.ToLowerInvariant()),
                    Description = condition.Name
                });
            }

            IconsVisibility = true;
        }
        catch (Exception ex)
        {
            Shell.Current.DisplayAlert("Erro", $"Erro ao carregar ícones de saúde: {ex.Message}", "OK");
        }
    }

    private string RemoverAcentos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalizado)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Replace(" ", "");
    }

    public async Task<List<Models.HealthConditions.Condition>> GetConditionsByPatient(int patientId)
    {
        return await _db.Connection.QueryAsync<Models.HealthConditions.Condition>(@"
            SELECT c.*
            FROM Condition c
            INNER JOIN PatientCondition pc ON pc.ConditionId = c.Id
            WHERE pc.PatientId = ?
            ORDER BY c.Name",
            patientId);
    }
}