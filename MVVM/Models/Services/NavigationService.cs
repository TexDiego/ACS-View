using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Models.Services
{
    internal class NavigationService : INavigationService
    {
        private string Condition = "";

        public void SetCondition(string condition)
        {
            Condition = condition;
        }

        public string GetCondition()
        {
            return Condition;
        }
    }
}