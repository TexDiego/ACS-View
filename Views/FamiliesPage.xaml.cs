using ACS_View.ViewModels;

namespace ACS_View.Views;

public partial class FamiliesPage : ContentPage, IQueryAttributable
{
    public FamiliesPage()
	{
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
        {
            int parsedId = (int)id;

            // Aqui você cria a ViewModel com o id
            BindingContext = new FamiliesViewModel(parsedId);
        }
    }
}