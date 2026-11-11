using ACS_View.Domain.Entities;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
namespace ACS_View.Views;

public partial class PersonsInfo : Popup
{
    private readonly PersonsInfoViewModel vm;
    private Window? owner;
    public PersonsInfo(PersonsInfoViewModel vm)
    {
        InitializeComponent(); this.vm = vm; BindingContext = vm;
#if WINDOWS
        AddCopyMenu(PatientNameLabel, () => vm.PersonInfo.Name);
        AddCopyMenu(BirthDateLabel, () => BirthDateLabel.Text);
        AddCopyMenu(MotherNameLabel, () => vm.PersonInfo.MotherName);
        AddCopyMenu(FatherNameLabel, () => vm.PersonInfo.FatherName);
        AddCopyMenu(CnsLabel, () => vm.MainSus);
        AddCopyMenu(NisLabel, () => vm.NisNumber);
        AddCopyMenu(AddressLabel, () => vm.AddressForClipboard);
        AddCopyMenu(AddressComplementLabel, () => vm.Complemento);
#endif
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        vm.PropertyChanged += OnModelChanged;
    }
#if WINDOWS
    private void AddCopyMenu(Label label, Func<string?> getValue)
    {
        var copy = new MenuFlyoutItem { Text = "Copiar" };
        copy.Clicked += async (_, _) => await vm.CopyInformationAsync(getValue());
        FlyoutBase.SetContextFlyout(label, new MenuFlyout { copy });
    }
#endif

    public void SetPatient(Patient patient) => vm.SetPatient(patient);
    private void OnLoaded(object? sender, EventArgs e)
    {
        owner = Window ?? Shell.Current?.Window;
        if(owner is not null) { owner.SizeChanged -= OnWindowSizeChanged; owner.SizeChanged += OnWindowSizeChanged; }
        Resize();
    }
    private void OnUnloaded(object? sender, EventArgs e)
    {
        if(owner is not null) owner.SizeChanged -= OnWindowSizeChanged;
        vm.PropertyChanged -= OnModelChanged;
    }
    private void OnWindowSizeChanged(object? sender, EventArgs e) => Resize();
    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(vm.IsHealthPanelVisible)) Resize();
    }
    public void Resize()
    {
        var window = owner ?? Window ?? Shell.Current?.Window;
        var availableWidth = Math.Max(1, (window?.Width ?? 400) - 40);
        var availableHeight = Math.Clamp((window?.Height ?? 780) - 64, 1, 700);
        PatientDetailsScroll.MaximumHeightRequest = Math.Max(1, availableHeight - 150);
        HealthDetailsScroll.MaximumHeightRequest = Math.Max(1, availableHeight - 180);
        var sideBySide = DeviceInfo.Platform == DevicePlatform.WinUI && availableWidth >= 860;
        var expanded = vm.IsHealthPanelVisible;
        WidthRequest = sideBySide && expanded ? Math.Min(1080, availableWidth) : Math.Min(460, availableWidth);
        Composition.ColumnDefinitions.Clear();
        Composition.ColumnDefinitions.Add(new ColumnDefinition { Width = sideBySide && expanded ? new GridLength(460) : GridLength.Star });
        if(sideBySide && expanded) Composition.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        Grid.SetColumn(HealthPanel, sideBySide && expanded ? 1 : 0);
        MainPanel.IsVisible = !expanded || sideBySide;
    }
    private async void OnCloseClicked(object? sender, EventArgs e) => await CloseAsync();
}
