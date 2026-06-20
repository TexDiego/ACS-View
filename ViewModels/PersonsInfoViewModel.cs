using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ACS_View.ViewModels;

public partial class PersonsInfoViewModel(IPersonsInfoService _infoService,
                                          IPatientCidRepository _patientCidRepository,
                                          ICidRepository _cidRepository,
                                          ISQLiteConditionsRepository _conditionsRepository) : BaseViewModel
{
    public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;
    private static readonly Dictionary<string, string> _chapterIconMap = new()
    {
        ["Algumas doenças infecciosas e parasitárias"] = "infeccoes.png",
        ["Neoplasias [tumores]"] = "cancer.png",
        ["Doenças do sangue e dos órgãos hematopoéticos e alguns transtornos imunitários"] = "hematologicas.png",
        ["Doenças endócrinas, nutricionais e metabólicas"] = "endocrinas.png",
        ["Transtornos mentais e comportamentais"] = "mental.png",
        ["Doenças do sistema nervoso"] = "nervoso.png",
        ["Doenças do olho e anexos"] = "oftalmologicas.png",
        ["Doenças do ouvido e da apófise mastóide"] = "ouvido.png",
        ["Doenças do aparelho circulatório"] = "cardiacas.png",
        ["Doenças do aparelho respiratório"] = "respiratorias.png",
        ["Doenças do aparelho digestivo"] = "gastrointestinais.png",
        ["Doenças da pele e do tecido subcutâneo"] = "dermatologicas.png",
        ["Doenças do sistema osteomuscular e do tecido conjuntivo"] = "musculoesqueleticas.png",
        ["Doenças do aparelho geniturinário"] = "geniturinario.png",
        ["Gravidez, parto e puerpério"] = "gestante.png",
        ["Algumas afecções originadas no período perinatal"] = "perinatal.png",
        ["Malformações congênitas, deformidades e anomalias cromossômicas"] = "geneticas.png",
        ["Sintomas, sinais e achados anormais de exames clínicos e de laboratório, não classificados em outra parte"] = "sintomas.png",
        ["Lesões, envenenamento e algumas outras conseqüências de causas externas"] = "lesoes.png"
    };
    private static readonly Dictionary<string, string> _conditionIconMap = new()
    {
        [HealthConditionCatalog.Gestante] = "gestante.png",
        [HealthConditionCatalog.Diabetes] = "diabetes.png",
        [HealthConditionCatalog.Hipertensao] = "hipertensao.png",
        [HealthConditionCatalog.Tuberculose] = "tuberculose.png",
        [HealthConditionCatalog.Hanseniase] = "hanseniase.png",
        [HealthConditionCatalog.Acamado] = "acamado.png",
        [HealthConditionCatalog.Domiciliado] = "domiciliado.png",
        [HealthConditionCatalog.CondicaoMental] = "doencasmentais.png",
        [HealthConditionCatalog.Fumante] = "fumante.png",
        [HealthConditionCatalog.Alcoolatra] = "alcoolatra.png",
        [HealthConditionCatalog.Deficiente] = "acessibilidade.png",
        [HealthConditionCatalog.PortadorCancer] = "cancer.png",
        [HealthConditionCatalog.BolsaFamilia] = "bolsafamilia.png",
        [HealthConditionCatalog.DependenteQuimico] = "dependentequimico.png",
        [HealthConditionCatalog.Imunodeficiente] = "hiv.png"
    };

    [ObservableProperty] private ObservableCollection<HealthIcon> icons = [];
    [ObservableProperty] private Patient personInfo;

    [ObservableProperty] private string endereco = "Sem endereço";
    [ObservableProperty] private string complemento = string.Empty;
    private int _loadVersion;

    public void SetPatient(Patient patient)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        PersonInfo = patient;
        Endereco = "Carregando endereço...";
        Complemento = string.Empty;
        Icons = [];

        _ = LoadPatientDetailsAsync(patient.Id, loadVersion);
    }

    private async Task LoadPatientDetailsAsync(int patientId, int loadVersion)
    {
        try
        {
            var addressTask = _infoService.GetEnderecoAsync(patientId);
            var complementTask = _infoService.GetComplementoAsync(patientId);
            var iconsTask = BuildHealthIconsAsync(patientId);

            await Task.WhenAll(addressTask, complementTask, iconsTask);

            if (loadVersion != _loadVersion)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Endereco = addressTask.Result;
                Complemento = complementTask.Result ?? string.Empty;
                Icons = new ObservableCollection<HealthIcon>(iconsTask.Result);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar informações do cadastro: {ex.Message}");

            if (loadVersion != _loadVersion)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Endereco = "Endereço não encontrado";
                Complemento = string.Empty;
                Icons = [];
            });
        }
    }

    private async Task<List<HealthIcon>> BuildHealthIconsAsync(int patientId)
    {
        var iconsList = new List<HealthIcon>();
        var seenSources = new HashSet<string>();

        var conditions = await _conditionsRepository.GetConditionsByPatientIdAsync(patientId);
        foreach (var condition in conditions)
        {
            var key = HealthConditionCatalog.GetKey(condition.Description);
            if (!_conditionIconMap.TryGetValue(key, out var source))
            {
                source = "outras.png";
            }

            if (seenSources.Add(source))
            {
                iconsList.Add(new HealthIcon { IconSource = source, Description = condition.Description });
            }
        }

        var cids = await _patientCidRepository.GetPatientCIDsByPatientId(patientId);
        if (cids == null || cids.Count == 0)
        {
            return iconsList;
        }

        // buscar apenas ids distintos
        var distinctIds = cids.Select(x => x.CidId).Distinct().ToList();

        // buscar capítulos em paralelo (await Task.WhenAll)
        var chapterTasks = distinctIds
            .Select(async id => (Id: id, Chapter: await _cidRepository.GetChapterBySubcategoryAsync(id)));
        var results = await Task.WhenAll(chapterTasks);
        var chaptersById = results.ToDictionary(r => r.Id, r => r.Chapter);

        foreach (var id in distinctIds)
        {
            var chapter = chaptersById[id];
            var desc = chapter?.Description ?? string.Empty;
            if (!_chapterIconMap.TryGetValue(desc, out var source))
                source = "outras.png";

            if (seenSources.Add(source))
                iconsList.Add(new HealthIcon { IconSource = source, Description = desc });
        }

        return iconsList;
    }
}
