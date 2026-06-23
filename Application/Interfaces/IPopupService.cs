using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces;

public interface IPopupService
{
    Task<PopupResultDto<T>> ShowAsync<T>(object popup, IDictionary<string, object>? parameters = null);
    Task ShowAsync(object popup);
}
