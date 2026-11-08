using ACS_View.ViewModels;
namespace ACS_View.Views;

// Shared compact section. BindableLayout avoids nested virtualized lists in forms.
public sealed class KinshipSection : ContentView
{
    public static readonly BindableProperty EditorProperty=BindableProperty.Create(nameof(Editor),typeof(KinshipEditorViewModel),typeof(KinshipSection),null,propertyChanged:(b,o,n)=>((KinshipSection)b).Build(n as KinshipEditorViewModel));
    public KinshipEditorViewModel? Editor{get=>(KinshipEditorViewModel?)GetValue(EditorProperty);set=>SetValue(EditorProperty,value);}
    private static Label Text(string value)=>new(){Text=value,LineBreakMode=LineBreakMode.WordWrap};
    private void Build(KinshipEditorViewModel? vm)
    {
        if(vm==null){Content=null;return;}
        vm.OpenEditorRequested=link=>Shell.Current.Navigation.PushModalAsync(new NavigationPage(new KinshipRelationshipPage(vm,link)));
        vm.NavigateToPerson ??= id=>Shell.Current.GoToAsync($"addregister?patientId={id}");
        var stack=new VerticalStackLayout{Spacing=8,BindingContext=vm};
        stack.Add(new Button{Text="Vínculos familiares ▾",Command=vm.ToggleDetailsCommand});
        var details=new VerticalStackLayout{Spacing=8};details.SetBinding(IsVisibleProperty,nameof(vm.ShowDetails));
        var summary=Text("");summary.SetBinding(Label.TextProperty,nameof(vm.Summary));
        stack.Add(summary);
        var error=Text("");error.SetBinding(Label.TextProperty,nameof(vm.Error));stack.Add(error);
        var add=new Button{Text="Adicionar vínculo",Command=vm.AddCommand,IsVisible=vm.CanEdit};details.Add(add);
        details.Add(Rows(vm,false));
        details.Add(new Button{Text="Parentescos identificados automaticamente ▾",Command=vm.ToggleDerivedCommand});
        var derived=Rows(vm,true);derived.SetBinding(IsVisibleProperty,nameof(vm.ShowDerived));details.Add(derived);
#if WINDOWS
        var tree=new Button{Text="♧ Árvore / Rede familiar",Command=vm.DrawCommand};tree.SetBinding(IsVisibleProperty,nameof(vm.CanDraw));ToolTipProperties.SetText(tree,"Abrir árvore genealógica e rede familiar");details.Add(tree);
#endif
        stack.Add(details);Content=stack;
    }
    private static VerticalStackLayout Rows(KinshipEditorViewModel vm,bool derived)
    {
        var list=new VerticalStackLayout{Spacing=8};BindableLayout.SetItemsSource(list,derived?vm.Derived:vm.Direct);
        BindableLayout.SetItemTemplate(list,new DataTemplate(()=>
        {
            var card=new VerticalStackLayout{Spacing=3};
            var title=Text("");title.SetBinding(Label.TextProperty,nameof(RelativeRow.Title));card.Add(title);
            var detail=Text("");detail.FontSize=12;detail.SetBinding(Label.TextProperty,nameof(RelativeRow.Detail));card.Add(detail);
            var actions=new FlexLayout{Wrap=Microsoft.Maui.Layouts.FlexWrap.Wrap};
            var open=new Button{Text="Consultar",Command=vm.OpenPersonCommand};open.SetBinding(Button.CommandParameterProperty,".");actions.Add(open);
            if(vm.CanEdit&&!derived)
            {
                var edit=new Button{Text="Editar",Command=vm.EditCommand};edit.SetBinding(Button.CommandParameterProperty,".");actions.Add(edit);
                var remove=new Button{Text="Remover",Command=vm.RemoveCommand};remove.SetBinding(Button.CommandParameterProperty,".");actions.Add(remove);
            }
            card.Add(actions);return card;
        }));return list;
    }
}
