#if WINDOWS
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace ACS_View.ViewModels;
public sealed partial class FamilyGraphViewModel(IKinshipService service) : BaseViewModel
{
    private KinshipSnapshot? snapshot;
    public int CenterId {get;private set;}
    public FamilyGraphOptions Options {get;set;}=new();
    [ObservableProperty] private FamilyGraph? graph;
    [ObservableProperty] private FamilyGraphNode? selected;
    [ObservableProperty] private string status="";
    public string SelectedConnections => Selected is null || Graph is null ? "" : string.Join(" • ",Graph.Edges
        .Where(e=>e.From==Selected.PatientId||e.To==Selected.PatientId)
        .Select(e=>$"{(e.From==Selected.PatientId?e.ReverseLabel:e.Label)}: {Graph.Nodes.First(n=>n.PatientId==(e.From==Selected.PatientId?e.To:e.From)).Name}"+(e.IsActive?"":" (encerrado)")));
    partial void OnSelectedChanged(FamilyGraphNode? value)=>OnPropertyChanged(nameof(SelectedConnections));
    private int version;
    private int loadVersion;
    public async Task LoadAsync(int id)
    {
        var token=++loadVersion;
        try{IsLoading=true;var s=await service.LoadAsync();if(token!=loadVersion)return;snapshot=s;CenterId=id;await RebuildAsync();}
        catch(Exception ex){Status=ex.Message;}finally{if(token==loadVersion)IsLoading=false;}
    }
    public async Task RebuildAsync()
    {
        if(snapshot==null)return;var token=++version;var s=snapshot;var options=Options;var center=CenterId;
        try{var result=await Task.Run(()=>FamilyGraphBuilder.Build(s,center,options));if(token!=version)return;Graph=result;Selected=result.Nodes.FirstOrDefault(n=>n.PatientId==center);OnPropertyChanged(nameof(SelectedConnections));Status=$"{result.Nodes.Count} pessoas conhecidas • {result.Edges.Count} vínculos"+(result.SimplifiedEdges>0?$" • {result.SimplifiedEdges} conexões redundantes recolhidas na árvore (disponíveis na rede)":"")+(result.Truncated?" • Limite visual atingido; reduza a profundidade.":"");}
        catch(Exception ex){Status=ex.Message;}
    }
    [RelayCommand] private async Task RecenterAsync(){if(Selected!=null){CenterId=Selected.PatientId;await RebuildAsync();}}
    [RelayCommand] private async Task OpenPersonAsync(){try{if(Selected!=null)await Shell.Current.GoToAsync($"addregister?patientId={Selected.PatientId}");}catch(Exception ex){Status=ex.Message;}}
}
#endif
