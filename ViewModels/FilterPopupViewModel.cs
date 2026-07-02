using ACS_View.Application.DTOs;
using ACS_View.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class FilterPopupViewModel : BaseViewModel
    {
        [ObservableProperty] private string minimumAge = string.Empty;
        [ObservableProperty] private string maximumAge = string.Empty;
        [ObservableProperty] private bool onlyBolsaFamilia;
        [ObservableProperty] private int filterBy = 0;
        [ObservableProperty] private int orderBy = 0;

        public ObservableCollection<SexFilterOption> SexOptions { get; } =
        [
            new(nameof(Sexo.Masculino)),
            new(nameof(Sexo.Feminino)),
            new(nameof(Sexo.Indeterminado))
        ];

        public ICommand ToggleSexCommand { get; }

        public FilterPopupViewModel()
        {
            ToggleSexCommand = new Command<SexFilterOption>(ToggleSex);
        }

        public FilterPopupViewModel(PatientListFilterDto filter)
            : this()
        {
            MinimumAge = filter.MinimumAge?.ToString() ?? string.Empty;
            MaximumAge = filter.MaximumAge?.ToString() ?? string.Empty;
            OnlyBolsaFamilia = filter.OnlyBolsaFamilia;
            FilterBy = (int)filter.SortBy;
            OrderBy = filter.SortDescending ? 1 : 0;

            var selectedSexes = filter.Sexes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var option in SexOptions)
            {
                option.IsSelected = selectedSexes.Contains(option.Value);
            }
        }

        public bool TryCreateFilter(string filterKey, out PatientListFilterDto filter, out string errorMessage)
        {
            filter = new PatientListFilterDto
            {
                FilterKey = string.IsNullOrWhiteSpace(filterKey) ? "ALL" : filterKey,
                OnlyBolsaFamilia = OnlyBolsaFamilia,
                Sexes = SexOptions
                    .Where(option => option.IsSelected)
                    .Select(option => option.Value)
                    .ToList(),
                SortBy = FilterBy == 1 ? PatientListSortOption.Age : PatientListSortOption.Name,
                SortDescending = OrderBy == 1
            };
            errorMessage = string.Empty;

            if (!TryReadAge(MinimumAge, "idade inicial", out var minimumAge, out errorMessage) ||
                !TryReadAge(MaximumAge, "idade final", out var maximumAge, out errorMessage))
            {
                return false;
            }

            if (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)
            {
                errorMessage = "A idade inicial deve ser menor ou igual à idade final.";
                return false;
            }

            filter.MinimumAge = minimumAge;
            filter.MaximumAge = maximumAge;
            return true;
        }

        private static void ToggleSex(SexFilterOption? option)
        {
            if (option is null)
            {
                return;
            }

            option.IsSelected = !option.IsSelected;
        }

        private static bool TryReadAge(string value, string fieldName, out int? age, out string errorMessage)
        {
            age = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (!int.TryParse(value.Trim(), out var parsedAge) || parsedAge < 0 || parsedAge > 120)
            {
                errorMessage = $"Informe uma {fieldName} entre 0 e 120.";
                return false;
            }

            age = parsedAge;
            return true;
        }
    }
}
