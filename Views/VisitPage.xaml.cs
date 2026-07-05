using ACS_View.Application.DTOs;
using ACS_View.ViewModels;
using CommunityToolkit.Maui.Views;

namespace ACS_View.Views;

public partial class VisitPage : Popup<VisitBatchRequestDto>
{
    private readonly VisitBatchPopupViewModel viewModel;

    public VisitPage(int houseId, int familyId, IEnumerable<VisitFamilyMemberOptionDto> people)
    {
        InitializeComponent();
        BindingContext = viewModel = new VisitBatchPopupViewModel(houseId, familyId, people);
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void SaveButton_Clicked(object sender, EventArgs e)
    {
        if (!viewModel.TryCreateRequest(out var request))
        {
            return;
        }

        await CloseAsync(request);
    }
}
