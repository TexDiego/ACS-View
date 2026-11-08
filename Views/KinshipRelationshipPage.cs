using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
namespace ACS_View.Views;

public sealed class KinshipRelationshipPage : ContentPage
{
    private readonly KinshipEditorViewModel editor;
    private readonly FamilyRelationship? original;
    private readonly Picker type=new(){Title="Tipo de vínculo"};
    private readonly Picker direction=new(){Title="A pessoa selecionada é",ItemsSource=new[]{"Ascendente / primeiro papel","Descendente / papel recíproco"},SelectedIndex=0};
    private readonly Picker nature=new(){Title="Natureza parental",ItemsSource=KinshipCatalog.Natures,SelectedIndex=3};
    private readonly Picker state=new(){Title="Situação",ItemsSource=new[]{"Ativo","Encerrado"},SelectedIndex=0};
    private readonly Picker reason=new(){Title="Motivo do encerramento",ItemsSource=KinshipCatalog.EndReasons,SelectedIndex=0};
    private readonly Entry forward=new(){Placeholder="Descrição do papel principal"}, reverse=new(){Placeholder="Descrição recíproca"};
    private readonly Entry start=new(){Placeholder="Início opcional: dd/MM/aaaa"},end=new(){Placeholder="Término opcional: dd/MM/aaaa"};
    private readonly SearchBar search=new(){Placeholder="Pesquisar pessoa do perfil"};
    private readonly CollectionView results=new(){HeightRequest=200,SelectionMode=SelectionMode.None};
    private readonly Label selected=new(){Text="Selecione um cadastro"},error=new();
    private CancellationTokenSource? searchCancellation;
    private Patient? person;
    private bool saving, leaving;
    private int searchVersion;
    private readonly Button save=new(){Text="Ver prévia e confirmar"};
    private readonly Button cancel=new(){Text="Cancelar"};
    public KinshipRelationshipPage(KinshipEditorViewModel editor,FamilyRelationship? original)
    {
        this.editor=editor;this.original=original;Title=original==null?"Adicionar vínculo":"Editar vínculo";
        type.ItemsSource=KinshipCatalog.Types.Select(t=>$"{t.Category} • {t.Forward} / {t.Reverse}").ToArray();
        type.SelectedIndex=Array.FindIndex(KinshipCatalog.Types,t=>t.Code==(original?.TypeCode??"parent"));
        results.ItemTemplate=new DataTemplate(()=>
        {
            var button=new Button{LineBreakMode=LineBreakMode.WordWrap,HorizontalOptions=LayoutOptions.Fill};
            button.SetBinding(Button.TextProperty,"Display");
            button.Clicked+=(_,_)=>
            {
                if(saving||leaving||button.BindingContext is not SearchResult result)return;
                if(result.Person.Id==editor.Patient.Id){error.Text="Selecione outro cadastro. Uma pessoa não pode ser cônjuge dela mesma.";return;}
                person=result.Person;selected.Text=result.Display;error.Text="";
            };
            return button;
        });
        search.TextChanged+=async(_,e)=>await SearchAsync(e.NewTextValue);
        save.Clicked+=async(_,_)=>await ConfirmAsync();
        cancel.Clicked+=async(_,_)=>await CloseAsync();
        Content=new ScrollView{Content=new VerticalStackLayout{Padding=20,Spacing=10,Children={new Label{Text="Defina o papel da pessoa pesquisada em relação ao cadastro aberto."},type,direction,nature,state,start,end,reason,forward,reverse,search,selected,results,error,save,cancel}}};
        type.SelectedIndexChanged+=(_,_)=>
        {
            // Let WinUI finish changing the picker selection before replacing visible fields.
            Dispatcher.Dispatch(()=>{if(!leaving)UpdateFields();});
        };
        UpdateFields();
        if(original!=null)
        {
            direction.SelectedIndex=original.FromPatientId==editor.Patient.Id?1:0;
            nature.SelectedIndex=Array.IndexOf(KinshipCatalog.Natures,original.Nature);state.SelectedIndex=original.IsActive?0:1;
            reason.SelectedIndex=Array.IndexOf(KinshipCatalog.EndReasons,original.EndReason);
            forward.Text=original.ForwardLabel;reverse.Text=original.ReverseLabel;start.Text=original.StartDate?.ToString("dd/MM/yyyy");end.Text=original.EndDate?.ToString("dd/MM/yyyy");
            _=LoadOriginalAsync(original);
        }
    }
    private async Task LoadOriginalAsync(FamilyRelationship r)
    {
        try{var s=await editor.Service.LoadAsync();if(leaving)return;var id=r.FromPatientId==editor.Patient.Id?r.ToPatientId:r.FromPatientId;if(s.People.TryGetValue(id,out var p)){person=p;selected.Text=$"{p.Name} • #{p.Id} • {p.BirthDate:dd/MM/yyyy}";}}
        catch(Exception ex){error.Text=ex.Message;}
    }
    private void UpdateFields()
    {
        if(type.SelectedIndex<0)return;var t=KinshipCatalog.Types[type.SelectedIndex];
        nature.IsVisible=t.Code=="parent";state.IsVisible=true;
        start.IsVisible=end.IsVisible=reason.IsVisible=t.Category=="Conjugal";
        forward.Text=t.Forward;reverse.Text=t.Reverse;
        forward.IsVisible=reverse.IsVisible=true;direction.IsVisible=!t.Symmetric;
    }
    private sealed record SearchResult(Patient Person,string Display);
    private async Task SearchAsync(string query)
    {
        if(leaving)return;var version=++searchVersion;
        searchCancellation?.Cancel();searchCancellation?.Dispose();var source=searchCancellation=new();
        try
        {
            await Task.Delay(300,source.Token);
            var people=await editor.Service.SearchAsync(query??"",editor.Patient.Id,source.Token);
            if(leaving || source.IsCancellationRequested || version!=searchVersion)return;
            results.ItemsSource=people.Where(p=>p.Id>0 && p.Id!=editor.Patient.Id).Select(p=>new SearchResult(p,$"{p.Name} • #{p.Id} • {p.BirthDate:dd/MM/yyyy} • Residência #{p.HouseId} / Família {p.FamilyId}")).ToArray();
        }
        catch(OperationCanceledException){}catch(Exception ex){if(!leaving&&version==searchVersion)error.Text=ex.Message;}
    }
    private static DateTime? Date(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        if(DateTime.TryParseExact(value.Trim(),"dd/MM/yyyy",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),System.Globalization.DateTimeStyles.None,out var date))return date;
        throw new InvalidOperationException("Informe a data no formato dd/MM/aaaa ou deixe em branco.");
    }
    private async Task ConfirmAsync()
    {
        if(saving||leaving)return;
        saving=true;save.IsEnabled=false;cancel.IsEnabled=false;error.Text="";
        try
        {
            if(person==null)throw new InvalidOperationException("Selecione a pessoa relacionada.");
            if(person.Id==editor.Patient.Id)throw new InvalidOperationException("Uma pessoa não pode ser cônjuge dela mesma. Selecione outro cadastro.");
            if(type.SelectedIndex<0||type.SelectedIndex>=KinshipCatalog.Types.Length)throw new InvalidOperationException("Selecione o tipo de vínculo.");
            var t=KinshipCatalog.Types[type.SelectedIndex];var r=original?.Copy()??new();
            r.TypeCode=t.Code;r.FromPatientId=direction.SelectedIndex==0?person.Id:editor.Patient.Id;r.ToPatientId=direction.SelectedIndex==0?editor.Patient.Id:person.Id;
            r.ForwardLabel=forward.Text?.Trim()??"";r.ReverseLabel=reverse.Text?.Trim()??"";r.Nature=nature.SelectedItem?.ToString()??"Não informado";
            r.IsActive=state.SelectedIndex==0;r.StartDate=t.Category=="Conjugal"?Date(start.Text):null;r.EndDate=t.Category=="Conjugal"?Date(end.Text):null;r.EndReason=reason.SelectedItem?.ToString()??"Não informado";
            await editor.StageAsync(r);await CloseAsync();
        }
        catch(Exception ex){if(!leaving)error.Text=ex.Message;}
        finally{saving=false;if(!leaving){save.IsEnabled=true;cancel.IsEnabled=true;}}
    }
    private async Task CloseAsync()
    {
        if(leaving)return;
        leaving=true;searchCancellation?.Cancel();
        try{await Navigation.PopModalAsync();}
        catch(Exception ex){leaving=false;error.Text=ex.Message;}
    }
    protected override bool OnBackButtonPressed()=>saving||base.OnBackButtonPressed();
    protected override void OnDisappearing(){++searchVersion;searchCancellation?.Cancel();base.OnDisappearing();}
}
