#if WINDOWS
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
using Microsoft.Maui.Graphics;
using Microsoft.UI.Xaml.Input;

namespace ACS_View.Views.Desktop;

public sealed class FamilyGraphPage : ContentPage,IQueryAttributable
{
    private readonly FamilyGraphViewModel vm;
    private readonly GraphicsView drawing=new(){HorizontalOptions=LayoutOptions.Fill,VerticalOptions=LayoutOptions.Fill};
    private readonly GraphDrawable drawable=new();
    private Microsoft.UI.Xaml.UIElement? native;
    private double zoom=1,panX,panY;
    private Windows.Foundation.Point last,start;
    private bool dragging,moved;
    private int patientId;
    private readonly Picker mode=new(){ItemsSource=new[]{"Árvore genealógica","Rede familiar"},SelectedIndex=0};
    private readonly Picker nature=new(){Title="Natureza",ItemsSource=new[]{"Todas"}.Concat(KinshipCatalog.Natures).ToArray(),SelectedIndex=0};
    private readonly Picker category=new(){Title="Categoria",ItemsSource=new[]{"Todas","Genealógico","Conjugal","Afinidade","Social"},SelectedIndex=0};
    private readonly Stepper up=new(){Minimum=0,Maximum=12,Value=3},down=new(){Minimum=0,Maximum=12,Value=2},depth=new(){Minimum=1,Maximum=12,Value=2};
    private readonly Button treeTab = new(), networkTab = new(), zoomValue = new();
    private readonly Grid toolbar = new(), filterFields = new(), selectionBar = new();
    private readonly HorizontalStackLayout cameraTools = new() { Spacing = 4, VerticalOptions = LayoutOptions.Center };
    private readonly HorizontalStackLayout selectionActions = new() { Spacing = 6, VerticalOptions = LayoutOptions.Center };
    private readonly VerticalStackLayout selectedSummary = new() { Spacing = 2, VerticalOptions = LayoutOptions.Center };
    private readonly Grid tabs = new() { ColumnSpacing = 4, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
    private Border natureField = null!, ancestorsField = null!, descendantsField = null!, categoryField = null!, depthField = null!;
    private readonly HorizontalStackLayout networkConnections = new() { Spacing = 6, IsVisible = false };
    private readonly ScrollView legendPanel = new() { IsVisible = false, WidthRequest = 310, MaximumHeightRequest = 340, Margin = 12, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start };
    private readonly ScrollView relationshipDetails = new() { IsVisible = false, MaximumHeightRequest = 110, Margin = new Thickness(0, 10, 0, 0) };
    private readonly List<GraphicsView> legendSamples = [];
    private Button detailsButton = null!, legendButton = null!, openButton = null!, recenterButton = null!;
    private bool? compactHeader, compactFilters, compactFooter;

    public FamilyGraphPage(FamilyGraphViewModel vm)
    {
        this.vm = vm; BindingContext = vm; Title = "Árvore genealógica";
        var root = new Grid { Padding = 16, RowSpacing = 12, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        Theme(root, BackgroundColorProperty, "PageBackground", "DarkPageBackground");

        ConfigureButton(treeTab, "Árvore genealógica"); ConfigureButton(networkTab, "Rede familiar");
        treeTab.Clicked += (_, _) => mode.SelectedIndex = 0;
        networkTab.Clicked += (_, _) => mode.SelectedIndex = 1;
        tabs.Add(treeTab, 0, 0); tabs.Add(networkTab, 1, 0);
        var segmented = Surface(tabs, 4, "SurfaceContainerHigh", "DarkSurfaceContainerHigh");
        segmented.StrokeThickness = 0; segmented.MinimumWidthRequest = 330;
        toolbar.ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)];
        toolbar.RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)];
        toolbar.ColumnSpacing = 16; toolbar.RowSpacing = 8;
        toolbar.Add(segmented, 0, 0); toolbar.Add(cameraTools, 1, 0);

        cameraTools.Add(ActionButton("−", "Diminuir zoom", () => Zoom(1 / 1.2, drawing.Width / 2, drawing.Height / 2), 38));
        ConfigureButton(zoomValue, "100%"); zoomValue.WidthRequest = 68;
        ToolTipProperties.SetText(zoomValue, "Restaurar zoom para 100%");
        zoomValue.Clicked += (_, _) => { Zoom(1 / zoom, drawing.Width / 2, drawing.Height / 2); };
        cameraTools.Add(zoomValue);
        cameraTools.Add(ActionButton("+", "Aumentar zoom", () => Zoom(1.2, drawing.Width / 2, drawing.Height / 2), 38));
        cameraTools.Add(ActionButton("Centralizar", "Centralizar a pessoa principal", Center));
        legendButton = ActionButton("Legenda", "Consultar os tipos de conexão e os gestos de navegação", () =>
        {
            legendPanel.IsVisible = !legendPanel.IsVisible;
            legendButton.Text = legendPanel.IsVisible ? "Fechar legenda" : "Legenda";
        });
        cameraTools.Add(legendButton);

        natureField = Field("Natureza parental", CompactPicker(nature));
        ancestorsField = Field("Gerações acima", GenerationControl(up));
        descendantsField = Field("Gerações abaixo", GenerationControl(down));
        categoryField = Field("Categoria", CompactPicker(category));
        depthField = Field("Profundidade da rede", GenerationControl(depth));
        filterFields.ColumnSpacing = 12; filterFields.RowSpacing = 10;
        filterFields.ColumnDefinitions = [new(new GridLength(2, GridUnitType.Star)), new(GridLength.Star), new(GridLength.Star)];
        filterFields.RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)];
        filterFields.Add(natureField); filterFields.Add(ancestorsField); filterFields.Add(descendantsField); filterFields.Add(categoryField); filterFields.Add(depthField);
        var allConnections = new CheckBox { IsChecked = false, VerticalOptions = LayoutOptions.Center, HeightRequest = 32, MinimumHeightRequest = 32 };
        Theme(allConnections, CheckBox.ColorProperty, "Primary", "DarkIconPrimary");
        allConnections.CheckedChanged += (_, e) => { drawable.ShowAllNetworkConnections = e.Value; drawing.Invalidate(); };
        var allLabel = Muted("Mostrar todas as conexões", 13); allLabel.VerticalOptions = LayoutOptions.Center;
        ToolTipProperties.SetText(allLabel, "Desmarcado: destaca os vínculos da pessoa selecionada. Marcado: mostra também as demais conexões da rede.");
        networkConnections.Add(allConnections); networkConnections.Add(allLabel); filterFields.Add(networkConnections);
        var header = new VerticalStackLayout { Spacing = 12, Children = { toolbar, filterFields } };
        root.Add(Surface(header, 14), 0, 0);

        drawing.Drawable = drawable;
        var status = Muted("", 12); status.LineBreakMode = LineBreakMode.TailTruncation; status.VerticalOptions = LayoutOptions.Center;
        status.SetBinding(Label.TextProperty, nameof(vm.Status)); status.SetBinding(ToolTipProperties.TextProperty, nameof(vm.Status));
        var hint = Muted("Arraste para mover · Roda para zoom", 12); hint.VerticalOptions = LayoutOptions.Center;
        var statusBar = new Grid { Padding = new Thickness(12, 8), ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        statusBar.Add(status, 0, 0); statusBar.Add(hint, 1, 0);
        statusBar.SizeChanged += (_, _) => hint.IsVisible = statusBar.Width >= 750;
        var viewport = new Grid(); viewport.Add(drawing); viewport.Add(legendPanel);
        legendPanel.Content = BuildLegend();
        var canvas = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        canvas.Add(statusBar, 0, 0); canvas.Add(viewport, 0, 1);
        root.Add(Surface(canvas, 0, "SurfaceMuted", "DarkSurfaceMuted"), 0, 1);

        var selectedName = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation };
        selectedName.SetBinding(Label.TextProperty, new Binding("Selected.Name") { TargetNullValue = "Selecione uma pessoa na árvore" });
        var selectedDetail = Muted("", 12); selectedDetail.LineBreakMode = LineBreakMode.TailTruncation;
        selectedDetail.SetBinding(Label.TextProperty, new Binding("Selected.Detail") { TargetNullValue = "Clique em um cartão para consultar o cadastro." });
        selectedSummary.Add(selectedName); selectedSummary.Add(selectedDetail);
        detailsButton = ActionButton("Vínculos ▾", "Mostrar ou recolher os vínculos da pessoa selecionada", () =>
        {
            relationshipDetails.IsVisible = !relationshipDetails.IsVisible;
            detailsButton.Text = relationshipDetails.IsVisible ? "Vínculos ▴" : "Vínculos ▾";
        });
        openButton = new Button { Command = vm.OpenPersonCommand }; ConfigureButton(openButton, "Abrir cadastro", true);
        recenterButton = new Button { Command = vm.RecenterCommand }; ConfigureButton(recenterButton, "Definir como centro");
        ToolTipProperties.SetText(recenterButton, "Reconstruir o gráfico com a pessoa selecionada no centro");
        selectionActions.Add(detailsButton); selectionActions.Add(recenterButton); selectionActions.Add(openButton);
        selectionBar.ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)];
        selectionBar.RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)]; selectionBar.ColumnSpacing = 14; selectionBar.RowSpacing = 8;
        selectionBar.Add(selectedSummary, 0, 0); selectionBar.Add(selectionActions, 1, 0);
        var connections = Muted("", 13); connections.SetBinding(Label.TextProperty, nameof(vm.SelectedConnections));
        relationshipDetails.Content = connections;
        root.Add(Surface(new VerticalStackLayout { Children = { selectionBar, relationshipDetails } }, 12), 0, 2);
        Content = root;
        root.SizeChanged += (_, _) => ArrangePanels(root.Width - root.Padding.HorizontalThickness);

        mode.SelectedIndexChanged += async (_, _) => { UpdateMode(); await Filters(); };
        nature.SelectedIndexChanged += async (_, _) => await Filters(); category.SelectedIndexChanged += async (_, _) => await Filters();
        up.ValueChanged += async (_, _) => await Filters(); down.ValueChanged += async (_, _) => await Filters(); depth.ValueChanged += async (_, _) => await Filters();
        vm.PropertyChanged += OnChanged; drawing.SizeChanged += OnSize;
        drawing.Loaded += OnLoaded; drawing.Unloaded += OnUnloaded;
        UpdateMode(); UpdateSelection();
    }

    private static Color ResourceColor(string key)
    {
        var resources=Microsoft.Maui.Controls.Application.Current!.Resources;
        if(resources.TryGetValue(key,out var value)&&value is Color color)return color;
        // PageBackground is also an existing Style key; resolve the color token by type.
        foreach(var dictionary in resources.MergedDictionaries)
            if(dictionary.TryGetValue(key,out value)&&value is Color token)return token;
        throw new InvalidOperationException($"Cor do tema não encontrada: {key}");
    }
    private static void Theme(BindableObject target, BindableProperty property, string light, string dark)
        => target.SetAppThemeColor(property, ResourceColor(light), ResourceColor(dark));
    private static Label Muted(string text, double size = 13)
    {
        var label = new Label { Text = text, FontSize = size };
        Theme(label, Label.TextColorProperty, "TextSecondary", "DarkTextSecondary"); return label;
    }
    private static Border Surface(View content, double padding, string light = "Surface", string dark = "DarkSurface")
    {
        var border = new Border { Content = content, Padding = padding, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 } };
        border.SetDynamicResource(Border.StrokeProperty, "BorderSoftBrush"); Theme(border, BackgroundColorProperty, light, dark); return border;
    }
    private static void ConfigureButton(Button button, string text, bool primary = false)
    {
        button.Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources[primary ? "PrimaryButton" : "GhostButton"];
        button.Text = text; button.FontSize = 13; button.CornerRadius = 8; button.Padding = new Thickness(12, 0);
        button.MinimumHeightRequest = 38; button.HeightRequest = 38; button.VerticalOptions = LayoutOptions.Center;
        if (!primary) Theme(button, Button.TextColorProperty, "IconPrimary", "DarkIconPrimary");
    }
    private static Button ActionButton(string text, string tooltip, Action action, double width = -1)
    {
        var button = new Button(); ConfigureButton(button, text); if (width > 0) button.WidthRequest = width;
        ToolTipProperties.SetText(button, tooltip); button.Clicked += (_, _) => action(); return button;
    }
    private static Picker CompactPicker(Picker picker)
    {
        picker.Title = null; picker.FontSize = 14; picker.MinimumHeightRequest = 40; picker.HeightRequest = 40; picker.Margin = 0; return picker;
    }
    private static Border Field(string title, View control)
    {
        SemanticProperties.SetDescription(control,title);
        var label = Muted(title, 12); label.FontAttributes = FontAttributes.Bold;
        var container = new VerticalStackLayout { Spacing = 2, Children = { label, control } };
        return Surface(container, 10, "SurfaceContainer", "DarkSurfaceContainer");
    }
    private static View GenerationControl(Stepper stepper)
    {
        var value = new Label { Text = ((int)stepper.Value).ToString(), FontSize = 14, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        var minus = ActionButton("−", "Reduzir uma geração", () => stepper.Value = Math.Max(stepper.Minimum, stepper.Value - 1), 36);
        var plus = ActionButton("+", "Expandir uma geração", () => stepper.Value = Math.Min(stepper.Maximum, stepper.Value + 1), 36);
        void Update() { value.Text = ((int)stepper.Value).ToString(); minus.IsEnabled = stepper.Value > stepper.Minimum; plus.IsEnabled = stepper.Value < stepper.Maximum; }
        stepper.ValueChanged += (_, _) => Update(); Update();
        var grid = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, HeightRequest = 40 };
        grid.Add(minus, 0, 0); grid.Add(value, 1, 0); grid.Add(plus, 2, 0); return grid;
    }
    private View BuildLegend()
    {
        var content = new VerticalStackLayout { Spacing = 10 };
        var heading = new Label { Text = "Legenda", FontAttributes = FontAttributes.Bold, FontSize = 16 };
        content.Add(heading);
        void Row(string text, float[]? dash, bool union = false)
        {
            var sample = new GraphicsView { WidthRequest = 38, HeightRequest = 22, VerticalOptions = LayoutOptions.Center, Drawable = new GraphLineSample(dash, union) };
            legendSamples.Add(sample);
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 10 };
            row.Add(sample, 0, 0); row.Add(Muted(text), 1, 0); content.Add(row);
        }
        Row("Parentalidade biológica", null); Row("Parentalidade adotiva", [8, 4]); Row("Parentalidade socioafetiva", [2, 4]);
        Row("Natureza não informada", [7, 3, 2, 3]); Row("União ativa", null, true); Row("Vínculo encerrado", [7, 5]);
        content.Add(Muted("Ramais compartilhados representam somente os pais registrados. Um grupo de coparentes não implica união conjugal.", 12));
        content.Add(Muted("Arraste o gráfico para mover. Use a roda do mouse ou os botões para ajustar o zoom.", 12));
        return Surface(content, 16);
    }
    private void UpdateMode()
    {
        var network = mode.SelectedIndex == 1; Title = network ? "Rede familiar" : "Árvore genealógica";
        ancestorsField.IsVisible = descendantsField.IsVisible = !network;
        categoryField.IsVisible = depthField.IsVisible = networkConnections.IsVisible = network;
        void Tab(Button button, bool selected)
        {
            Theme(button, BackgroundColorProperty, selected ? "PrimaryContainer" : "SurfaceContainerHigh", selected ? "DarkPrimaryContainer" : "DarkSurfaceContainerHigh");
            Theme(button, Button.TextColorProperty, selected ? "OnPrimaryContainer" : "TextSecondary", selected ? "DarkOnPrimaryContainer" : "DarkTextSecondary");
            button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
            button.HorizontalOptions = LayoutOptions.Fill;
        }
        Tab(treeTab, !network); Tab(networkTab, network);
        compactFilters = null; ArrangePanels(Math.Max(0, Width - 32));
    }
    private void ArrangePanels(double width)
    {
        if (width <= 0) return;
        bool narrowHeader = width < 870, narrowFields = width < 640, narrowFooter = width < 900;
        if (compactHeader != narrowHeader)
        {
            compactHeader = narrowHeader;
            Grid.SetRow(cameraTools, narrowHeader ? 1 : 0); Grid.SetColumn(cameraTools, narrowHeader ? 0 : 1);
            Grid.SetColumnSpan(cameraTools, narrowHeader ? 2 : 1); Grid.SetColumnSpan((BindableObject)toolbar.Children[0], narrowHeader ? 2 : 1);
            cameraTools.HorizontalOptions = narrowHeader ? LayoutOptions.Start : LayoutOptions.End;
        }
        if (compactFilters != narrowFields)
        {
            compactFilters = narrowFields; filterFields.ColumnDefinitions.Clear();
            for (int i = 0; i < (narrowFields ? 2 : 3); i++) filterFields.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(!narrowFields && i == 0 ? 2 : 1, GridUnitType.Star)));
            Grid.SetRow(natureField, 0); Grid.SetColumn(natureField, 0); Grid.SetColumnSpan(natureField, narrowFields ? 2 : 1);
            foreach (var field in new[] { ancestorsField, categoryField }) { Grid.SetRow(field, narrowFields ? 1 : 0); Grid.SetColumn(field, narrowFields ? 0 : 1); }
            foreach (var field in new[] { descendantsField, depthField }) { Grid.SetRow(field, narrowFields ? 1 : 0); Grid.SetColumn(field, narrowFields ? 1 : 2); }
            Grid.SetRow(networkConnections, narrowFields ? 2 : 1); Grid.SetColumn(networkConnections, 0); Grid.SetColumnSpan(networkConnections, narrowFields ? 2 : 3);
        }
        if (compactFooter != narrowFooter)
        {
            compactFooter = narrowFooter;
            Grid.SetRow(selectionActions, narrowFooter ? 1 : 0); Grid.SetColumn(selectionActions, narrowFooter ? 0 : 1);
            Grid.SetColumnSpan(selectionActions, narrowFooter ? 2 : 1); Grid.SetColumnSpan(selectedSummary, narrowFooter ? 2 : 1);
            selectionActions.HorizontalOptions = narrowFooter ? LayoutOptions.Start : LayoutOptions.End;
        }
    }
    private void UpdateSelection()
    {
        detailsButton.IsEnabled = openButton.IsEnabled = recenterButton.IsEnabled = vm.Selected is not null;
    }
    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        UpdateDrawing(); foreach (var sample in legendSamples) sample.Invalidate();
    }
    public void ApplyQueryAttributes(IDictionary<string,object> query){if(query.TryGetValue("patientId",out var id))patientId=Convert.ToInt32(id);}
    protected override async void OnAppearing(){base.OnAppearing();vm.PropertyChanged-=OnChanged;vm.PropertyChanged+=OnChanged;Microsoft.Maui.Controls.Application.Current!.RequestedThemeChanged-=OnThemeChanged;Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged+=OnThemeChanged;await vm.LoadAsync(patientId);Center();OnLoaded(this,EventArgs.Empty);}
    protected override void OnDisappearing(){Detach();vm.PropertyChanged-=OnChanged;Microsoft.Maui.Controls.Application.Current!.RequestedThemeChanged-=OnThemeChanged;base.OnDisappearing();}
    private async Task Filters(){vm.Options=new(mode.SelectedIndex==1,(int)up.Value,(int)down.Value,(int)depth.Value,nature.SelectedItem?.ToString()??"Todas",category.SelectedItem?.ToString()??"Todas");await vm.RebuildAsync();}
    private void OnChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e){if(e.PropertyName==nameof(vm.Graph))Center();else UpdateDrawing();UpdateSelection();}
    private void OnSize(object? sender,EventArgs e){legendPanel.MaximumHeightRequest=Math.Max(100,Math.Min(340,drawing.Height-24));Center();}
    private void Center(){var n=vm.Graph?.Nodes.FirstOrDefault(x=>x.PatientId==vm.CenterId);panX=drawing.Width/2-(n?.X??0)*zoom;panY=drawing.Height/2-(n?.Y??0)*zoom;UpdateDrawing();}
    private void UpdateDrawing(){drawable.Network=vm.Options.Network;drawable.Graph=vm.Graph;drawable.Selected=vm.Selected?.PatientId??vm.CenterId;drawable.Zoom=(float)zoom;drawable.PanX=(float)panX;drawable.PanY=(float)panY;drawable.Palette=GraphPalette.Current();zoomValue.Text=$"{zoom:P0}";drawing.Invalidate();}
    private void Zoom(double factor,double x,double y){var old=zoom;zoom=Math.Clamp(zoom*factor,.15,3);panX=x-(x-panX)*zoom/old;panY=y-(y-panY)*zoom/old;UpdateDrawing();}
    private void OnLoaded(object? sender,EventArgs e)
    {
        Detach();native=drawing.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if(native==null)return;native.PointerPressed+=Pressed;native.PointerMoved+=Moved;native.PointerReleased+=Released;native.PointerCanceled+=Canceled;native.PointerWheelChanged+=Wheel;
    }
    private void OnUnloaded(object? sender,EventArgs e)=>Detach();
    private void Detach(){if(native==null)return;native.PointerPressed-=Pressed;native.PointerMoved-=Moved;native.PointerReleased-=Released;native.PointerCanceled-=Canceled;native.PointerWheelChanged-=Wheel;native.ReleasePointerCaptures();native=null;dragging=false;}
    private void Pressed(object sender,PointerRoutedEventArgs e){if(native==null)return;start=last=e.GetCurrentPoint(native).Position;dragging=true;moved=false;native.CapturePointer(e.Pointer);e.Handled=true;}
    private void Moved(object sender,PointerRoutedEventArgs e){if(!dragging||native==null)return;var p=e.GetCurrentPoint(native).Position;panX+=p.X-last.X;panY+=p.Y-last.Y;last=p;if(Math.Abs(p.X-start.X)+Math.Abs(p.Y-start.Y)>5)moved=true;UpdateDrawing();e.Handled=true;}
    private void Released(object sender,PointerRoutedEventArgs e)
    {
        if(native==null)return;if(!moved&&dragging){var p=e.GetCurrentPoint(native).Position;var x=(p.X-panX)/zoom;var y=(p.Y-panY)/zoom;vm.Selected=vm.Graph?.Nodes.FirstOrDefault(n=>Math.Abs(n.X-x)<=100&&Math.Abs(n.Y-y)<=38);UpdateDrawing();}dragging=false;native.ReleasePointerCapture(e.Pointer);e.Handled=true;
    }
    private void Canceled(object sender,PointerRoutedEventArgs e){dragging=false;native?.ReleasePointerCapture(e.Pointer);}
    private void Wheel(object sender,PointerRoutedEventArgs e){if(native==null)return;var p=e.GetCurrentPoint(native);Zoom(p.Properties.MouseWheelDelta>0?1.15:1/1.15,p.Position.X,p.Position.Y);e.Handled=true;}
}
internal sealed class GraphDrawable : IDrawable
{
    public bool Network;public bool ShowAllNetworkConnections;public FamilyGraph? Graph;public int Selected;public float Zoom=1,PanX,PanY;public GraphPalette Palette=GraphPalette.Current();
    public void Draw(ICanvas c,RectF dirty)
    {
        c.FillColor=Palette.Canvas;c.FillRectangle(dirty);if(Graph==null)return;
        c.SaveState();c.Translate(PanX,PanY);c.Scale(Zoom,Zoom);
        foreach(var group in Graph.Groups)
        {
            c.FillColor=Palette.Group;
            c.FillRoundedRectangle(group.X,group.Y,group.Width,96,14);
        }
        foreach(var stroke in Graph.Strokes)
        {
            bool highlighted=stroke.People.Contains(Selected);
            if(Network&&!ShowAllNetworkConnections&&!highlighted)continue;
            c.Alpha=Network&&!highlighted?.18f:1;
            c.StrokeColor=stroke.Category is "Afinidade" or "Social" ? Palette.Muted : Palette.Primary;
            c.StrokeSize=highlighted?2.5f:1.5f;
            c.StrokeDashPattern=!stroke.IsActive?[7,5]:stroke.Category=="Conjugal"?null:stroke.Nature switch{"Biológico"=>null,"Adotivo"=>[8,4],"Socioafetivo"=>[2,4],_=>[7,3,2,3]};
            var a=stroke.Start;var b=stroke.End;c.DrawLine(a.X,a.Y,b.X,b.Y);
            if(stroke.Category=="Conjugal")c.DrawLine(a.X,a.Y+3,b.X,b.Y+3);
            if(stroke.Arrow)
            {
                var dx=b.X-a.X;var dy=b.Y-a.Y;var length=(float)Math.Sqrt(dx*dx+dy*dy);
                if(length>.01f){var ux=dx/length;var uy=dy/length;c.StrokeDashPattern=null;c.DrawLine(b.X,b.Y,b.X-ux*8+uy*4,b.Y-uy*8-ux*4);c.DrawLine(b.X,b.Y,b.X-ux*8-uy*4,b.Y-uy*8+ux*4);}
            }
        }
        c.Alpha=1;
        foreach(var annotation in Graph.Annotations)
        {
            if(!annotation.AlwaysVisible && (Zoom<.7f || !annotation.People.Contains(Selected)))continue;
            c.FillColor=Palette.Canvas;c.FillRoundedRectangle(annotation.X-110,annotation.Y-10,220,22,4);
            c.FontColor=Palette.Text;c.FontSize=10;c.DrawString(annotation.Text,annotation.X-108,annotation.Y-10,216,22,HorizontalAlignment.Center,VerticalAlignment.Center);
        }
        c.StrokeDashPattern=null;
        foreach(var n in Graph.Nodes)
        {
            float screenX=n.X*Zoom+PanX,screenY=n.Y*Zoom+PanY;
            if(screenX+110*Zoom<dirty.Left||screenX-110*Zoom>dirty.Right||screenY+45*Zoom<dirty.Top||screenY-45*Zoom>dirty.Bottom)continue;
            c.FillColor=n.PatientId==Selected?Palette.Selection:Palette.Card;c.FillRoundedRectangle(n.X-100,n.Y-38,200,76,10);
            c.StrokeColor=n.PatientId==Selected?Palette.Primary:Palette.Outline;c.StrokeSize=n.PatientId==Selected?3:1;c.DrawRoundedRectangle(n.X-100,n.Y-38,200,76,10);
            c.FontColor=Palette.Text;c.FontSize=13;c.DrawString(n.Name,n.X-94,n.Y-30,188,36,HorizontalAlignment.Center,VerticalAlignment.Center);
            c.FontColor=Palette.Muted;c.FontSize=10;c.DrawString(n.Detail,n.X-94,n.Y+8,188,24,HorizontalAlignment.Center,VerticalAlignment.Center);
        }
        c.RestoreState();
    }
}
internal sealed record GraphPalette(Color Canvas,Color Group,Color Card,Color Selection,Color Text,Color Muted,Color Outline,Color Primary)
{
    public static GraphPalette Current()
    {
        var resources=Microsoft.Maui.Controls.Application.Current!.Resources;
        bool dark=Microsoft.Maui.Controls.Application.Current.RequestedTheme==AppTheme.Dark;
        Color C(string light,string night)=>(Color)resources[dark?night:light];
        return new(C("SurfaceMuted","DarkSurfaceMuted"),C("SurfaceContainerHigh","DarkSurfaceContainerHigh"),C("Surface","DarkSurface"),
            C("PrimaryContainer","DarkPrimaryContainer"),C("TextPrimary","DarkTextPrimary"),C("TextSecondary","DarkTextSecondary"),
            C("OutlineVariant","DarkOutlineVariant"),C("IconPrimary","DarkIconPrimary"));
    }
}
internal sealed class GraphLineSample(float[]? dash,bool union) : IDrawable
{
    public void Draw(ICanvas canvas,RectF rect)
    {
        canvas.StrokeColor=GraphPalette.Current().Primary;canvas.StrokeSize=2;canvas.StrokeDashPattern=dash;
        canvas.DrawLine(2,10,rect.Width-2,10);if(union)canvas.DrawLine(2,14,rect.Width-2,14);
    }
}
#endif
