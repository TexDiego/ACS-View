using ACS_View.Domain.Entities;
using ACS_View.Application.Interfaces;
using ACS_View.ViewModels;

namespace ACS_View.Views;

internal sealed class PersonsInfoPopupService(Func<PersonsInfoViewModel> createViewModel, IPopupService popupService) : IPersonsInfoPopupService
{
    public async Task ShowAsync(Patient patient)
    {
        var popup = new PersonsInfo(createViewModel());

        popup.SetPatient(patient);
        await popupService.ShowAsync(popup);
    }
}
