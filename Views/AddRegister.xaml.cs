using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using System.Diagnostics;

namespace ACS_View.Views;

public partial class AddRegister : ContentPage, IQueryAttributable
{
    private readonly AddRegisterViewModel _viewModel;
    private int? _patientId;
    private bool _loaded;
    private bool _isLoading;

    public AddRegister(
        IPatientService patientService,
        ICidRepository cidRepo,
        IPatientCidRepository patientCid,
        ISQLiteConditionsRepository conditionsRepository,
        IPatientBolsaFamiliaRepository bolsaFamiliaRepository,
        IPatientInsulinDependencyRepository insulinDependencyRepository,
        IPregnancyService pregnancyService,
        ICareNotificationService careNotificationService,
        IPopupService popupService,
        IPatientPhoneService phoneService)
    {
        var timer = Stopwatch.StartNew();
        InitializeComponent();
        BindingContext = _viewModel = new AddRegisterViewModel(patientService, cidRepo, patientCid, conditionsRepository, bolsaFamiliaRepository, insulinDependencyRepository, pregnancyService, careNotificationService, popupService, phoneService);
        Debug.WriteLine($"[Cadastro.Load] Construir pagina e bindings: {timer.Elapsed.TotalMilliseconds:F1} ms");
    }

    private bool? _sexOptionsHorizontal;

    private void OnEditPhoneClicked(object? sender, EventArgs e) => PhoneEntry.Focus();

    private void OnEditSusClicked(object? sender, EventArgs e) => SusEntry.Focus();

    private void OnPrimarySusCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton { BindingContext: ACS_View.Domain.ValueObjects.SusNumber number }
            && _viewModel.SusEditor.Numbers.Contains(number))
            _viewModel.SusEditor.PrimaryNumber = number;
    }

    private void OnPrimaryPhoneCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton { BindingContext: ACS_View.Domain.ValueObjects.Phone phone }
            && _viewModel.PhoneEditor.Phones.Contains(phone))
            _viewModel.PhoneEditor.PrimaryPhone = phone;
    }

    private void OnSexOptionsSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Grid grid || grid.Width <= 0)
        {
            return;
        }

        // Three compact choices on typical screens; two rows at narrow widths.
        var horizontal = grid.Width >= 300;
        if (_sexOptionsHorizontal == horizontal) return;
        _sexOptionsHorizontal = horizontal;
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        if (!horizontal) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var index = 0; index < (horizontal ? 3 : 2); index++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var index = 0; index < grid.Children.Count; index++)
        {
            grid.SetRow(grid.Children[index], !horizontal && index == 2 ? 1 : 0);
            grid.SetColumn(grid.Children[index], !horizontal && index == 2 ? 0 : index);
            grid.SetColumnSpan(grid.Children[index], !horizontal && index == 2 ? 2 : 1);
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("patientId", out var patientId))
        {
            SetPatientId(Convert.ToInt32(patientId));
        }

        if (query.TryGetValue("record", out var record) && record is Patient p)
        {
            SetPatientId(p.Id);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        InputFocusGuard.ClearTextInputFocus(this);
        _ = LoadPageDataAsync();
    }

    protected override void OnDisappearing()
    {
        InputFocusGuard.ClearTextInputFocus(this);
        base.OnDisappearing();
    }

    private async Task LoadPageDataAsync()
    {
        if (_loaded || _isLoading)
        {
            return;
        }

        _isLoading = true;
        var timer = Stopwatch.StartNew();
        try
        {
            if (_patientId is > 0)
            {
                await _viewModel.LoadPatient(_patientId.Value);
            }
            else
            {
                _viewModel.InitializeNewPatient();
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _viewModel.ShowLoadErrorAsync();
        }
        finally
        {
            _isLoading = false;
            Debug.WriteLine($"[Cadastro.Load] {(_patientId is > 0 ? "Edicao" : "Novo")} pronto: {timer.Elapsed.TotalMilliseconds:F1} ms");
        }
    }

    private void SetPatientId(int patientId)
    {
        if (_patientId == patientId)
        {
            return;
        }

        _patientId = patientId;
        _loaded = false;
    }
}
