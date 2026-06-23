using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class ConditionPopupViewModel(ICidRepository _cidRepo) : BaseViewModel
    {
        public double Width => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) - 20;

        private List<CidSubcategory> AllConditions = [];

        [ObservableProperty] private List<HealthConditions> healthCategories = [];
        [ObservableProperty] private ObservableCollection<HealthConditions> searchedConditions = [];

        [ObservableProperty] private string searchText = string.Empty;

        private CancellationTokenSource cts = new CancellationTokenSource();

        public ICommand SwitchSelected => new Command<HealthConditions>(Switch);

        internal async Task LoadPatientCid(List<HealthConditions> cids)
        {
            if (cids == null) return;

            // Ensure conditions are loaded before trying to map patient CIDs
            if (AllConditions == null || AllConditions.Count == 0)
            {
                await LoadConditionsAsync();
            }

            foreach (var c in cids)
            {
                var cid = AllConditions.Where(x => x.Description == c.Name).FirstOrDefault();

                if (cid != null)
                {
                    var hc = HealthCategories.Where(x => x.Name == cid.Description).FirstOrDefault();
                    hc?.Selected = true;
                }
            }
        }

        internal async Task LoadConditionsAsync()
        {
            AllConditions = await _cidRepo.GetAllSubcategories();

            // Clear once before adding all items
            HealthCategories.Clear();
            foreach (var condition in AllConditions)
            {
                HealthCategories.Add(new HealthConditions
                {
                    Name = condition.Description,
                    CID = condition.Code,
                    Selected = false
                });
            }
        }

        // Debounced search: wait 300ms after the last change before applying filter
        async partial void OnSearchTextChanged(string value)
        {
            // cancel previous debounce
            cts.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            if (string.IsNullOrEmpty(value))
            {
                SearchedConditions.Clear();
                return;
            }

            try
            {
                await Task.Delay(300, token);
            }
            catch (TaskCanceledException)
            {
                return; // new input arrived
            }

            value = value.Replace(".", "");

            var filtered = HealthCategories.Where(x =>
            !string.IsNullOrEmpty(x.Name) &&
                         (x.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                       || x.CID.Contains(value, StringComparison.OrdinalIgnoreCase))
                         .ToList().Take(5);

            // Ensure UI update runs on main thread
            RunOnMainThread(() =>
            {
                SearchedConditions.Clear();
                foreach (var condition in filtered)
                {
                    SearchedConditions.Add(condition);
                }
            });
        }

        private void Switch(HealthConditions condition)
        {
            // Find the matching health category and toggle its Selected state safely
            var hc = HealthCategories.FirstOrDefault(x => x.Name == condition?.Name);
            if (hc == null) return;

            hc.Selected = !hc.Selected;
            var isNowSelected = hc.Selected;
        }
    }
}
