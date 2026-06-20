namespace ACS_View.UseCases.DTOs
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
        public PatientListSortOption SortBy { get; set; } = PatientListSortOption.Name;
        public bool SortDescending { get; set; }

        public bool HasListFilters =>
            MinimumAge.HasValue ||
            MaximumAge.HasValue ||
            SortBy != PatientListSortOption.Name ||
            SortDescending;

        public PatientListFilterDto Clone()
        {
            return new PatientListFilterDto
            {
                FilterKey = FilterKey,
                MinimumAge = MinimumAge,
                MaximumAge = MaximumAge,
                SortBy = SortBy,
                SortDescending = SortDescending
            };
        }
    }
}
