namespace ACS_View.Application.DTOs
{
    public class PatientImportResultDto
    {
        public List<ImportAuditEntryDto> Entries { get; set; } = [];
        public int? DuplicateDetectionVersion { get; set; }
        public int DuplicateRowCount => Entries.Count(e => e.EntityType == "Paciente" && e.DuplicateOfRowNumber.HasValue);
        public int DuplicatePatientCount => Entries.Where(e => e.EntityType == "Paciente" && e.DuplicateOfRowNumber.HasValue && e.RecordId.HasValue).Select(e => e.RecordId).Distinct().Count();
        public int MergedCount { get; set; }
        public List<string> Details { get; set; } = [];
        public int ImportedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int IgnoredCount { get; set; }
        public List<string> Errors { get; set; } = [];

        public int TotalProcessed => ImportedCount + UpdatedCount + IgnoredCount;
    }
}
