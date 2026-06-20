using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class VaccinesInfo : Popup<bool>
{
	private readonly VaccinesInfoViewModel _viewModel;
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

    private void VaccineChecked_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _vaccineStatus = e.Value;
        Console.WriteLine(_vaccineStatus);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync(_vaccineStatus);
    }
}
