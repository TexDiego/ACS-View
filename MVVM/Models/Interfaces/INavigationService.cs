namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface INavigationService
    {
        void SetCondition(string condition);
        string GetCondition();
    }
}