using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;
using System.Diagnostics;

namespace ACS_View.Views;

public partial class ConditionsPopup : Popup<object>, IQueryAttributable
{
	private readonly ConditionPopupViewModel viewModel;

    internal ConditionsPopup(ICidRepository repo)
    {
        InitializeComponent();
        BindingContext = viewModel = new ConditionPopupViewModel(repo);
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if(query.TryGetValue("record", out var record))
            if (record is List<HealthConditions> list)
                await viewModel.LoadPatientCid(list);
    }

    public void OnCloseButtonClicked(object sender, EventArgs e)
	{
		try
        {
            var result = viewModel.HealthCategories.Where(x => x.Selected).ToList();
            Debug.WriteLine($"Closing popup with {result.Count} results.", "[Popup DEBUG]");
		    this.CloseAsync(result);
		}
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            this.CloseAsync();
        }
    }
}