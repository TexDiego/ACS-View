namespace ACS_View.Application.Interfaces;

public interface IMainThreadDispatcher
{
    void Dispatch(Action action);
    void Dispatch(Func<Task> action);
}
