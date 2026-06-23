using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IPersonsInfoPopupService
{
    Task ShowAsync(Patient patient);
}
