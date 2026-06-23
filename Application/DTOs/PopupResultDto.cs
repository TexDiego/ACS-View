namespace ACS_View.Application.DTOs;

public sealed class PopupResultDto<T>
{
    public bool WasDismissed { get; init; }
    public T? Result { get; init; }
}
