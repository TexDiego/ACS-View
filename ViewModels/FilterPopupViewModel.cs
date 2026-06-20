using ACS_View.UseCases.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices;
using System;

namespace ACS_View.ViewModels
{
    internal partial class FilterPopupViewModel : BaseViewModel
    {
        // Calcula uma largura adequada para o popup com fallback para evitar exceções em plataformas onde DeviceDisplay
        // não esteja disponível ou retorne valores inesperados.
        public double MaxScreenWidth
        {
            get
            {
                try
                {
                    var info = DeviceDisplay.MainDisplayInfo;
                    var density = info.Density <= 0 ? 1 : info.Density;
                    var width = (info.Width / density) - 20;
                    return Math.Min(Math.Max(width, 0), 520);
                }
                catch
                {
                    // Fallback seguro
                    return 520;
                }
            }
        }

        [ObservableProperty] private string minimumAge = string.Empty;
        [ObservableProperty] private string maximumAge = string.Empty;
        [ObservableProperty] private int filterBy = 0;
        [ObservableProperty] private int orderBy = 0;

        public FilterPopupViewModel()
        {
        }

        public FilterPopupViewModel(PatientListFilterDto filter)
        {
            MinimumAge = filter.MinimumAge?.ToString() ?? string.Empty;
            MaximumAge = filter.MaximumAge?.ToString() ?? string.Empty;
            FilterBy = (int)filter.SortBy;
            OrderBy = filter.SortDescending ? 1 : 0;
        }

        public bool TryCreateFilter(string filterKey, out PatientListFilterDto filter, out string errorMessage)
        {
            filter = new PatientListFilterDto
            {
                FilterKey = string.IsNullOrWhiteSpace(filterKey) ? "ALL" : filterKey,
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
