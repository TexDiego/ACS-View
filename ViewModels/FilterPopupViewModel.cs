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
        [ObservableProperty] private bool withoutSusNumber;
        [ObservableProperty] private bool withoutPhone;
        [ObservableProperty] private bool withoutMotherName;
        [ObservableProperty] private bool withoutFatherName;
        [ObservableProperty] private bool withoutHouse;
        [ObservableProperty] private int filterBy = 0;
        [ObservableProperty] private int orderBy = 0;

        public ObservableCollection<SexFilterOption> SexOptions { get; } =
        [
            new(nameof(Sexo.Masculino)),
            new(nameof(Sexo.Feminino)),
            new(nameof(Sexo.Indeterminado))
        ];

        public ICommand ToggleSexCommand { get; }
        public ICommand ToggleBolsaFamiliaCommand { get; }
        public ICommand ToggleMissingInformationCommand { get; }

        public double PopupHeight
        {
            get
            {
                try
                {
                    var info = DeviceDisplay.MainDisplayInfo;
                    return Math.Min(680, Math.Max(240, info.Height / Math.Max(1, info.Density) - 80));
                }
                catch { return 600; }
            }
        }

        public FilterPopupViewModel()
        {
            ToggleSexCommand = new Command<SexFilterOption>(ToggleSex);
            ToggleBolsaFamiliaCommand = new Command(ToggleBolsaFamilia);
            ToggleMissingInformationCommand = new Command<string>(field =>
            {
                switch (field)
                {
                    case nameof(WithoutSusNumber): WithoutSusNumber = !WithoutSusNumber; break;
                    case nameof(WithoutPhone): WithoutPhone = !WithoutPhone; break;
                    case nameof(WithoutMotherName): WithoutMotherName = !WithoutMotherName; break;
                    case nameof(WithoutFatherName): WithoutFatherName = !WithoutFatherName; break;
                    case nameof(WithoutHouse): WithoutHouse = !WithoutHouse; break;
                }
            });
        }

        private void ToggleBolsaFamilia()
        {
            OnlyBolsaFamilia = !OnlyBolsaFamilia;
        }

        public FilterPopupViewModel(PatientListFilterDto filter)
            : this()
        {
            MinimumAge = filter.MinimumAge?.ToString() ?? string.Empty;
            MaximumAge = filter.MaximumAge?.ToString() ?? string.Empty;
            OnlyBolsaFamilia = filter.OnlyBolsaFamilia;
            WithoutSusNumber = filter.WithoutSusNumber;
            WithoutPhone = filter.WithoutPhone;
            WithoutMotherName = filter.WithoutMotherName;
            WithoutFatherName = filter.WithoutFatherName;
            WithoutHouse = filter.WithoutHouse;
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
                WithoutSusNumber = WithoutSusNumber,
                WithoutPhone = WithoutPhone,
                WithoutMotherName = WithoutMotherName,
                WithoutFatherName = WithoutFatherName,
                WithoutHouse = WithoutHouse,
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
