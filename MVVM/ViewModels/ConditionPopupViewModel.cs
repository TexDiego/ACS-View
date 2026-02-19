using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class ConditionPopupViewModel : ConditionCategoryVM
    {
        public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;

        public ICommand SwitchConditionState => new Command<int>((id) => SwitchCondition(id));


        internal ConditionPopupViewModel(ConditionCategoryVM condition)
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadConditions(condition));
        }

        private async Task LoadConditions(ConditionCategoryVM condition)
        {
            Conditions = condition.Conditions;

            Name = condition.Name;

            foreach (var c in Conditions)
                Debug.WriteLine(c.Id);
        }


        private void SwitchCondition(int id)
        {
            var condition = Conditions.FirstOrDefault(c => c.Id == id);
            if (condition != null)
                condition.IsSelected = !condition.IsSelected;
        }
    }
}