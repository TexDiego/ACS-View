using ACS_View.MVVM.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.MVVM.Views;

public partial class VaccinesInfo : Popup
{
	private VaccinesInfoViewModel _viewModel;
    private bool _vaccineStatus;

	public VaccinesInfo(string vaccineName, bool vaccineChecked)
	{
		InitializeComponent();

        Console.WriteLine(vaccineChecked);

        _viewModel = new VaccinesInfoViewModel(vaccineName);

        BindingContext = _viewModel;

        VaccineChecked.IsChecked = vaccineChecked;
        _vaccineStatus = vaccineChecked;
    }

    protected override Task OnDismissedByTappingOutsideOfPopup(CancellationToken token = default)
    {
#if DEBUG
        Console.WriteLine("Fechando popup com o valor " + _vaccineStatus);
#endif
        ResultWhenUserTapsOutsideOfPopup = _vaccineStatus;
        return base.OnDismissedByTappingOutsideOfPopup(token);
    }

    private void VaccineChecked_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _vaccineStatus = e.Value;
        Console.WriteLine(_vaccineStatus);
    }
}