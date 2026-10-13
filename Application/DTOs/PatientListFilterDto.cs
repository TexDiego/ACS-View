namespace ACS_View.Application.DTOs
{
    public enum PatientListSortOption
    {
        Name = 0,
        Age = 1
    }

    public sealed class PatientListFilterDto
    {
        public string FilterKey { get; set; } = "ALL";
        public int? MinimumAge { get; set; }
        public int? MaximumAge { get; set; }
        public bool OnlyBolsaFamilia { get; set; }
        public bool WithoutSusNumber { get; set; }
        public bool WithoutPhone { get; set; }
        public bool WithoutMotherName { get; set; }
        public bool WithoutFatherName { get; set; }
        public bool WithoutHouse { get; set; }
        public List<string> Sexes { get; set; } = [];
        public PatientListSortOption SortBy { get; set; } = PatientListSortOption.Name;
        public bool SortDescending { get; set; }

        public bool HasListFilters =>
            MinimumAge.HasValue ||
            MaximumAge.HasValue ||
            OnlyBolsaFamilia ||
            WithoutSusNumber || WithoutPhone || WithoutMotherName || WithoutFatherName || WithoutHouse ||
            Sexes.Count > 0 ||
            SortBy != PatientListSortOption.Name ||
            SortDescending;

        public PatientListFilterDto Clone()
        {
            return new PatientListFilterDto
            {
                FilterKey = FilterKey,
                MinimumAge = MinimumAge,
                MaximumAge = MaximumAge,
                OnlyBolsaFamilia = OnlyBolsaFamilia,
                WithoutSusNumber = WithoutSusNumber,
                WithoutPhone = WithoutPhone,
                WithoutMotherName = WithoutMotherName,
                WithoutFatherName = WithoutFatherName,
                WithoutHouse = WithoutHouse,
                Sexes = [.. Sexes],
                SortBy = SortBy,
                SortDescending = SortDescending
            };
        }
    }
}
