using System.Collections.ObjectModel;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels;

public sealed record RelativeRow(KnownRelative Relative, Patient Person, string Detail, FamilyRelationship? Declared)
{
    public string Title => $"{(Declared is null ? Relative.Label : Declared.FromPatientId==Person.Id ? Declared.ForwardLabel : Declared.ReverseLabel)} — {Person.Name}";
}
public sealed partial class KinshipEditorViewModel(IKinshipService service, bool readOnly = false) : BaseViewModel
{
    public bool CanEdit => !readOnly;
    public bool CanDraw => OperatingSystem.IsWindows() && Patient.Id>0;
    public Patient Patient { get; private set; } = new();
    private KinshipSnapshot? snapshot;
    private readonly List<FamilyRelationship> pending=[];
    private readonly List<int> removed=[];
    private int loadVersion;
    private int nextPendingId = -1;
    private int refreshVersion;
    public ObservableCollection<RelativeRow> Direct { get; }=[];
    public ObservableCollection<RelativeRow> Derived { get; }=[];
    public IKinshipService Service => service;
    [ObservableProperty] private bool showDerived;
    [ObservableProperty] private bool showDetails = !readOnly;
    [RelayCommand] private void ToggleDetails()=>ShowDetails=!ShowDetails;
    [ObservableProperty] private string summary="Vínculos conhecidos pelo aplicativo; podem não representar todos os parentes.";
    [ObservableProperty] private string error="";
    public async Task LoadAsync(Patient patient)
    {
        var version=++loadVersion;++refreshVersion;Patient=patient;snapshot=null;pending.Clear();removed.Clear();Direct.Clear();Derived.Clear();Summary="Carregando vínculos conhecidos…";Error="";OnPropertyChanged(nameof(CanDraw));
        try
        {
            var loaded=await service.LoadAsync();if(version!=loadVersion)return;
            snapshot=loaded;pending.Clear();removed.Clear();await RefreshAsync();Error="";
        }
        catch(Exception ex){if(version==loadVersion)Error=ex.Message;}
    }
    private int WorkingId => Patient.Id>0?Patient.Id:(snapshot?.People.Keys.DefaultIfEmpty(0).Max()??0)+1;
    private KinshipSnapshot Working()
    {
        var s=snapshot??throw new InvalidOperationException("Aguarde o carregamento dos vínculos.");
        var people=s.People.ToDictionary(x=>x.Key,x=>x.Value);people[WorkingId]=Patient;
        var links=s.Relationships.Where(x=>!removed.Contains(x.Id)&&!pending.Any(p=>p.Id>0&&p.Id==x.Id)).Select(x=>x.Copy()).ToList();
        links.AddRange(pending.Select(x=>{var r=x.Copy();if(r.FromPatientId==0)r.FromPatientId=WorkingId;if(r.ToPatientId==0)r.ToPatientId=WorkingId;r.UserId=s.UserId;KinshipEngine.Normalize(r);return r;}));
        return new(s.UserId,people,links);
    }
    private async Task RefreshAsync()
    {
        if(snapshot==null)return;
        var s=Working();var personId=WorkingId;var token=++refreshVersion;var relatives=await Task.Run(()=>KinshipEngine.Query(s,personId));if(token!=refreshVersion)return;
        Direct.Clear();Derived.Clear();
        foreach(var r in relatives.OrderBy(r=>r.TypeCode).ThenBy(r=>s.People[r.PatientId].Name))
        {
            var links=s.Relationships.Where(x=>r.DeclaredIds.Contains(x.Id) && (x.FromPatientId==WorkingId||x.ToPatientId==WorkingId)).ToArray();
            var detail=r.Provenance+(r.SiblingClassification!=""?" • "+r.SiblingClassification:"");
            if(r.Paths.Count>0)detail+=" • "+string.Join(" / ",r.Paths.Select(p=>string.Join(" → ",p.Natures)).Distinct());
            if(links.Length==0)Derived.Add(new(r,s.People[r.PatientId],detail,null));
            foreach(var link in links)
            {
                var d=detail+" • "+(link.TypeCode=="parent"?link.Nature:KinshipCatalog.Get(link.TypeCode).Category);
                if(KinshipCatalog.Get(link.TypeCode).Category=="Conjugal")d+=$" • {(link.IsActive?"Ativo":"Encerrado")} • {link.StartDate?.ToString("dd/MM/yyyy")??"Início desconhecido"} — {link.EndDate?.ToString("dd/MM/yyyy")??"Término desconhecido"} • {link.EndReason}";
                if(!link.IsActive && KinshipCatalog.Get(link.TypeCode).Category!="Conjugal")d+=" • Encerrado";
                if(link.TypeCode is "grandparent" or "greatgrandparent" && !r.IsDerived)d+=" • Geração intermediária não conhecida";
                Direct.Add(new(r,s.People[r.PatientId],d,link));
            }
        }
        Summary="Vínculos conhecidos pelo aplicativo (não a totalidade dos parentes): "+string.Join("; ",relatives.GroupBy(r=>r.TypeCode).Select(g=>$"{g.First().Label}: {g.Select(r=>r.PatientId).Distinct().Count()}"));
    }
    [RelayCommand] private void ToggleDerived()=>ShowDerived=!ShowDerived;
    [RelayCommand] private async Task AddAsync()=>await OpenEditorAsync(null);
    [RelayCommand] private async Task EditAsync(RelativeRow row)=>await OpenEditorAsync(row?.Declared);
    private async Task OpenEditorAsync(FamilyRelationship? link)
    {
        if(!CanEdit||snapshot==null)return;
        if(link!=null){link=link.Copy();if(link.FromPatientId==WorkingId)link.FromPatientId=Patient.Id;if(link.ToPatientId==WorkingId)link.ToPatientId=Patient.Id;}
        Error="";try{if(OpenEditorRequested!=null)await OpenEditorRequested(link);}
        catch(Exception ex){Error="Não foi possível abrir o vínculo: "+ex.Message;}
    }
    public async Task StageAsync(FamilyRelationship input)
    {
        if(input.FromPatientId==input.ToPatientId)throw new InvalidOperationException("Não é possível vincular uma pessoa a ela mesma. Selecione outro cadastro.");
        var r=input.Copy();if(r.FromPatientId==WorkingId)r.FromPatientId=Patient.Id;if(r.ToPatientId==WorkingId)r.ToPatientId=Patient.Id;
        var oldPending=pending.ToList();
        if(r.Id>0)pending.RemoveAll(x=>x.Id==r.Id);else if(input.Id<0)pending.RemoveAll(x=>x.Id==input.Id);
        if(r.Id==0)r.Id=nextPendingId--;
        pending.Add(r);
        try
        {
            var after=Working();
            var before=snapshot! with{People=after.People};
            var actual=after.Relationships.Last();
            var preview=await Task.Run(()=>KinshipEngine.Preview(before,after,actual));
            var text=$"{after.People[actual.FromPatientId].Name}: {actual.ForwardLabel} de {after.People[actual.ToPatientId].Name}. Recíproco: {actual.ReverseLabel}. Natureza: {actual.Nature}. Situação: {(actual.IsActive?"ativo":"encerrado")}.\nReciprocidade obrigatória pela mesma relação.\n"+string.Join("\n",preview.Effects.Take(25))+"\n"+string.Join("\n",preview.Warnings);
            if(preview.Effects.Count>25)text+=$"\nMais {preview.Effects.Count-25} atualizações demonstráveis.";
            if(!await DisplayConfirmationAsync("Prévia dos vínculos",text,"Confirmar","Cancelar")){pending.Clear();pending.AddRange(oldPending);return;}
            foreach(var suggestion in preview.Suggestions)
                if(await DisplayConfirmationAsync("Sugestão opcional",$"Confirmar {after.People[suggestion.FromPatientId].Name} como {suggestion.ForwardLabel} de {after.People[suggestion.ToPatientId].Name}? Irmandade sozinha não comprova este vínculo.","Incluir","Ignorar"))
                {
                    var optional=suggestion.Copy();if(optional.FromPatientId==WorkingId)optional.FromPatientId=Patient.Id;if(optional.ToPatientId==WorkingId)optional.ToPatientId=Patient.Id;optional.Id=nextPendingId--;pending.Add(optional);
                    var optionalSnapshot=Working();await Task.Run(()=>KinshipEngine.Validate(optionalSnapshot));
                }
            await RefreshAsync();Error="";
        }
        catch{pending.Clear();pending.AddRange(oldPending);throw;}
    }
    [RelayCommand] private async Task RemoveAsync(RelativeRow row)
    {
        if(row?.Declared==null||!CanEdit)return;
        try
        {
            var link=row.Declared;
            var after=Working() with{Relationships=Working().Relationships.Where(r=>r.Id!=link.Id).ToArray()};
            var preview=await Task.Run(()=>KinshipEngine.Preview(Working(),after,link));
            if(!await DisplayConfirmationAsync("Remover vínculo", "Somente este vínculo será removido. Pessoas e outros vínculos serão preservados.\n"+string.Join("\n",preview.Effects.Take(25)),"Remover","Cancelar"))return;
            pending.RemoveAll(x=>x.Id==link.Id);if(link.Id>0)removed.Add(link.Id);await RefreshAsync();
        }
        catch(Exception ex){Error=ex.Message;}
    }
    public async Task ValidatePendingAsync(){if(pending.Count>0||removed.Count>0){var s=Working();await Task.Run(()=>KinshipEngine.Validate(s));}}
    public async Task CommitAsync(Patient saved)
    {
        var changes=pending.Select(x=>{var r=x.Copy();if(r.Id<0)r.Id=0;return r;}).ToArray();
        await service.SaveRegistrationAsync(saved,changes,removed);pending.Clear();removed.Clear();await LoadAsync(saved);
    }
    [RelayCommand] private async Task OpenPersonAsync(RelativeRow row)
    {
        if(row==null)return;
        // The host popup closes itself before navigation through the supplied action.
        try{if(NavigateToPerson!=null)await NavigateToPerson(row.Person.Id);}
        catch(Exception ex){Error=ex.Message;}
    }
    public Func<FamilyRelationship?,Task>? OpenEditorRequested { get; set; }
    public Func<int,Task>? NavigateToPerson { get; set; }
    [RelayCommand] private async Task DrawAsync()
    {
#if WINDOWS
        try{if(NavigateToTree!=null)await NavigateToTree(Patient.Id);
        else await Shell.Current.GoToAsync($"FamilyGraphPage?patientId={Patient.Id}");}
        catch(Exception ex){Error=ex.Message;}
#endif
    }
    public Func<int,Task>? NavigateToTree { get; set; }
}
