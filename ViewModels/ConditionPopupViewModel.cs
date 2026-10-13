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
        [ObservableProperty] private List<HealthConditions> healthCategories = [];
        [ObservableProperty] private ObservableCollection<HealthConditions> searchedConditions = [];

        [ObservableProperty] private string searchText = string.Empty;

        private CancellationTokenSource? cts;

        public ICommand SwitchSelected => new Command<HealthConditions>(Switch);

        internal Task LoadPatientCid(List<HealthConditions> cids)
        {
            // Keep independent copies: dismissing the popup must not edit the form.
            HealthCategories = cids.Select(c => new HealthConditions
            {
                Name = c.Name, CID = c.CID, IsCid = true, Selected = true
            }).ToList();
            return Task.CompletedTask;
        }

        // Debounced search: wait 300ms after the last change before applying filter
        async partial void OnSearchTextChanged(string value)
        {
            // cancel previous debounce
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            if (string.IsNullOrWhiteSpace(value))
            {
                SearchedConditions.Clear();
                return;
            }

            try
            {
                await Task.Delay(300, token);
                var results = await _cidRepo.SearchCidAsync(value.Replace(".", ""), null, 0, 5, token);
                token.ThrowIfCancellationRequested();
                SearchedConditions.Clear();
                foreach (var result in results)
                {
                    var condition = HealthCategories.FirstOrDefault(c =>
                        string.Equals(c.CID, result.Code, StringComparison.OrdinalIgnoreCase));
                    SearchedConditions.Add(condition ?? new HealthConditions
                    {
                        Name = result.Description, CID = result.Code, IsCid = true
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (!token.IsCancellationRequested)
                {
                    await DisplayAlertAsync("Erro", "Não foi possível buscar as condições de saúde.");
                }
            }
        }

        private void Switch(HealthConditions condition)
        {
            if (condition is null) return;
            condition.Selected = !condition.Selected;
            if (condition.Selected && !HealthCategories.Contains(condition))
                HealthCategories.Add(condition);
            else if (!condition.Selected)
                HealthCategories.Remove(condition);
        }
    }
}
