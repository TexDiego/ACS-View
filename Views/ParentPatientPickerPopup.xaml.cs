using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class ParentPatientPickerPopup : Popup<PatientListItemDto>
{
    private readonly ParentPatientPickerViewModel _viewModel;
    private bool _isClosing;

    internal ParentPatientPickerPopup(IPatientService patientService, int? excludedPatientId, string title)
    {
        InitializeComponent();
        BindingContext = _viewModel = new ParentPatientPickerViewModel(patientService, excludedPatientId, title);
        Loaded += (_, _) => InputFocusGuard.ClearTextInputFocus(this);
        _ = _viewModel.LoadAsync();
    }

    private async void OnPatientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isClosing || e.CurrentSelection.FirstOrDefault() is not PatientListItemDto selectedPatient)
        {
            return;
        }

        _isClosing = true;
#if WINDOWS
        // WinUI still updates the selected item after raising SelectionChanged.
        // Closing inline disconnects its handler before that update can finish.
        await Dispatcher.DispatchAsync(() => CloseAsync(selectedPatient));
#else
        await CloseAsync(selectedPatient);
#endif
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        await CloseAsync();
    }
}
