using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class PersonsInfoViewModel(IPersonsInfoService _infoService,
                                          IPatientService _patientService,
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
        ["Algumas afecções originadas no período perinatal"] = "outras.png",
        ["Malformações congênitas, deformidades e anomalias cromossômicas"] = "geneticas.png",
        ["Sintomas, sinais e achados anormais de exames clínicos e de laboratório, não classificados em outra parte"] = "outras.png",
        ["Lesões, envenenamento e algumas outras conseqüências de causas externas"] = "outras.png"
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
    private static readonly IReadOnlyList<CidIconRule> _specificCidIconRules =
    [
        new("alopecia.png", "Alopecia", cid => IsInRange(cid.Code, "L63", "L65") || HasAnyTerm(cid, "alopecia")),
        new("arritmia.png", "Arritmia", cid => IsInRange(cid.Code, "I44", "I49") || HasAnyTerm(cid, "arritmia", "fibrilacao", "flutter", "taquicardia", "bradicardia")),
        new("artrite.png", "Artrite", cid => IsInRange(cid.Code, "M05", "M14") || HasAnyTerm(cid, "artrite")),
        new("artrose.png", "Artrose", cid => IsInRange(cid.Code, "M15", "M19") || HasAnyTerm(cid, "artrose", "osteoartrose")),
        new("asma.png", "Asma", cid => IsInRange(cid.Code, "J45", "J46") || HasAnyTerm(cid, "asma", "estado de mal asmatico")),
        new("dislipidemias.png", "Dislipidemia", cid => IsInRange(cid.Code, "E78", "E78") || HasAnyTerm(cid, "dislipidemia", "hiperlipidemia", "hipercolesterolemia")),
        new("doencaarterialcoronaria.png", "Doença arterial coronariana", cid => IsInRange(cid.Code, "I20", "I25") || HasAnyTerm(cid, "angina", "infarto", "isquemica do coracao", "coronar")),
        new("hepatopatas.png", "Hepatopatia", cid => IsInRange(cid.Code, "K70", "K77") || HasAnyTerm(cid, "figado", "hepatica", "hepatite", "cirrose")),
        new("insuficienciacardiaca.png", "Insuficiência cardíaca", cid => IsInRange(cid.Code, "I50", "I50") || HasAnyTerm(cid, "insuficiencia cardiaca")),
        new("neurodivergencias.png", "Neurodivergência", cid => IsInRange(cid.Code, "F80", "F98") || HasAnyTerm(cid, "autismo", "autista", "asperger", "hipercinetico", "deficit de atencao", "desenvolvimento psicologico")),
        new("cadeirante.png", "Cadeirante", IsWheelchairRelatedCid),
        new("obesidade.png", "Obesidade", cid => IsInRange(cid.Code, "E66", "E66") || HasAnyTerm(cid, "obesidade")),
        new("pneumonia.png", "Pneumonia", cid => IsInRange(cid.Code, "J12", "J18") || HasAnyTerm(cid, "pneumonia")),
        new("psoriase.png", "Psoríase", cid => IsInRange(cid.Code, "L40", "L40") || HasAnyTerm(cid, "psoriase")),
        new("renais.png", "Doença renal", cid => IsInRange(cid.Code, "N00", "N19") || HasAnyTerm(cid, "renal", "rim", "rins", "nefrit", "nefros")),
        new("rosacea.png", "Rosácea", cid => IsInRange(cid.Code, "L71", "L71") || HasAnyTerm(cid, "rosacea")),
        new("vitiligo.png", "Vitiligo", cid => IsInRange(cid.Code, "L80", "L80") || HasAnyTerm(cid, "vitiligo"))
    ];

    [ObservableProperty] private ObservableCollection<HealthIcon> icons = [];
    [ObservableProperty] private Patient personInfo;
    [ObservableProperty] private bool hasMotherPatientLink;
    [ObservableProperty] private bool hasFatherPatientLink;

    [ObservableProperty] private string endereco = "Sem endereço";
    [ObservableProperty] private string complemento = string.Empty;
    private int _loadVersion;

    public ICommand OpenLinkedParentCommand => new Command<object?>(async id => await OpenLinkedPatientAsync(id));

    public void SetPatient(Patient patient)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);

        PersonInfo = patient;
        HasMotherPatientLink = patient.MotherPatientId is > 0;
        HasFatherPatientLink = patient.FatherPatientId is > 0;
        Endereco = "Carregando endereço...";
        Complemento = string.Empty;
        Icons = [];

        _ = LoadPatientDetailsAsync(patient.Id, loadVersion);
    }

    private async Task OpenLinkedPatientAsync(object? id)
    {
        var patientId = id switch
        {
            int value => value,
            long value => (int)value,
            string value when int.TryParse(value, out var parsed) => parsed,
            _ => 0
        };

        if (patientId <= 0)
        {
            return;
        }

        var linkedPatient = await _patientService.GetPatientById(patientId);
        if (linkedPatient is null)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => SetPatient(linkedPatient));
    }

    private async Task LoadPatientDetailsAsync(int patientId, int loadVersion)
    {
        try
        {
            var addressTask = _infoService.GetAddressInfoAsync(patientId);
            var iconsTask = BuildHealthIconsAsync(patientId);

            await Task.WhenAll(addressTask, iconsTask);

            if (loadVersion != _loadVersion)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Endereco = addressTask.Result.Endereco;
                Complemento = addressTask.Result.Complemento ?? string.Empty;
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
        var patient = await _patientService.GetPatientById(patientId);

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

        if (patient?.BirthDate <= DateTime.Today.AddYears(-60))
        {
            if (seenSources.Add("idoso.png"))
            {
                iconsList.Add(new HealthIcon { IconSource = "idoso.png", Description = "Idoso" });
            }
        }

        if (patient?.BirthDate > DateTime.Today.AddYears(-12))
        {
            if (seenSources.Add("criancas.png"))
            {
                iconsList.Add(new HealthIcon { IconSource = "criancas.png", Description = "Criança" });
            }
        }

        var cids = await _patientCidRepository.GetPatientCIDsByPatientId(patientId);
        if (cids == null || cids.Count == 0)
        {
            return iconsList;
        }

        var distinctIds = cids.Select(x => x.CidId).Distinct().ToList();
        var cidInfoTasks = distinctIds.Select(async id => (
            Id: id,
            Subcategory: await _cidRepository.GetSubcategoryById(id),
            Chapter: await _cidRepository.GetChapterBySubcategoryAsync(id)));
        var cidInfos = await Task.WhenAll(cidInfoTasks);
        var cidInfoById = cidInfos.ToDictionary(info => info.Id);

        foreach (var id in distinctIds)
        {
            var cidInfo = cidInfoById[id];
            var source = "outras.png";
            var desc = cidInfo.Chapter?.Description ?? string.Empty;

            if (cidInfo.Subcategory is not null &&
                TryGetSpecificCidIcon(cidInfo.Subcategory, out var specificSource, out var specificDescription))
            {
                source = specificSource;
                desc = specificDescription;
            }
            else if (!_chapterIconMap.TryGetValue(desc, out source))
            {
                source = "outras.png";
            }

            if (seenSources.Add(source))
                iconsList.Add(new HealthIcon { IconSource = source, Description = desc });
        }

        return iconsList;
    }

    private static bool TryGetSpecificCidIcon(CidSubcategory cid, out string iconSource, out string description)
    {
        foreach (var rule in _specificCidIconRules)
        {
            if (!rule.Matches(cid))
            {
                continue;
            }

            iconSource = rule.IconSource;
            description = rule.Description;
            return true;
        }

        iconSource = string.Empty;
        description = string.Empty;
        return false;
    }

    private static bool HasAnyTerm(CidSubcategory cid, params string[] terms)
    {
        var normalizedText = SearchTextNormalizer.Normalize($"{cid.Description} {cid.Code} {cid.CategoryCode}");
        return terms.Any(term => normalizedText.Contains(SearchTextNormalizer.Normalize(term), StringComparison.Ordinal));
    }

    private static bool IsWheelchairRelatedCid(CidSubcategory cid)
    {
        return IsInCategory(cid, "G82", "R26") ||
               IsCode(cid, "M623") ||
               IsCode(cid, "Z993") ||
               HasAnyTerm(
                   cid,
                   "cadeira de rodas",
                   "dependencia de cadeira de rodas",
                   "paraplegia",
                   "paraplegica",
                   "tetraplegia",
                   "marcha paralitica",
                   "dificuldade para andar",
                   "anormalidades da marcha");
    }

    private static bool IsInCategory(CidSubcategory cid, params string[] categoryCodes)
    {
        return categoryCodes.Any(category =>
            string.Equals(cid.CategoryCode, category, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCode(CidSubcategory cid, string code)
    {
        return string.Equals(NormalizeCidCode(cid.Code), NormalizeCidCode(code), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCidCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim().Replace(".", string.Empty).ToUpperInvariant();
    }

    private static bool IsInRange(string? code, string start, string end)
    {
        if (!TryParseCidCode(code, out var parsedCode) ||
            !TryParseCidCode(start, out var parsedStart) ||
            !TryParseCidCode(end, out var parsedEnd))
        {
            return false;
        }

        return parsedCode.Letter == parsedStart.Letter &&
               parsedCode.Letter == parsedEnd.Letter &&
               parsedCode.Number >= parsedStart.Number &&
               parsedCode.Number <= parsedEnd.Number;
    }

    private static bool TryParseCidCode(string? code, out (char Letter, int Number) parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length < 3 || !char.IsLetter(normalized[0]))
        {
            return false;
        }

        var digits = new string(normalized.Skip(1).TakeWhile(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var number))
        {
            return false;
        }

        parsed = (normalized[0], number);
        return true;
    }

    private sealed record CidIconRule(string IconSource, string Description, Func<CidSubcategory, bool> Matches);
}
