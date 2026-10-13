using SQLite;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Domain.Entities
{
    public class Patient
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RowRevision { get; set; }
        public int FamilyId { get; set; } = -1;
        public int HouseId { get; set; } = -1;

        private string susNumber = string.Empty;

        // Keeps the SQLite column and text bindings compatible with old records.
        public string SusNumber
        {
            get => susNumber;
            // Materializing an old row must not silently discard its original evidence.
            set { susNumber = value ?? string.Empty; SusNumberRecords = null; }
        }

        [Ignore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IReadOnlyList<string> SusNumbers
        {
            get => SusNumberRecords?.OrderByDescending(n => n.IsPrimary).ThenBy(n => n.OriginalOrder).Select(n => n.Number).ToArray() ?? SusNumberSet.Parse(susNumber);
            set { susNumber = SusNumberSet.Join(value); SusNumberRecords = null; }
        }

        [Ignore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IReadOnlyList<PatientSusNumber>? SusNumberRecords { get; set; }

        [Ignore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string PrimarySusNumber => SusNumbers.FirstOrDefault() ?? string.Empty;

        public void AddSusNumbers(string? numbers)
        {
            var additions = SusNumberSet.Parse(numbers).Except(SusNumbers).ToArray();
            if (additions.Length == 0) return;
            // Retain the exact old text while appending imported numbers.
            var existingExtra = SusNumbers.Except(SusNumberSet.Parse(SusNumber));
            SusNumber = string.Join("; ", new[] { SusNumber }.Where(n => !string.IsNullOrWhiteSpace(n)).Concat(existingExtra).Concat(additions));
        }

        public bool HasSusNumber(string? number) => SusNumbers.Intersect(SusNumberSet.Parse(number)).Any();
        public string? FamilyResponsibleSus { get; set; }
        public int? MotherPatientId { get; set; }
        public int? FatherPatientId { get; set; }
        public int? FamilyResponsiblePatientId { get; set; }
        public string Sexo { get; set; } = "Indeterminado";
        public string Name { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
        public string SearchMotherName { get; set; } = string.Empty;
        public string SearchFatherName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } = DateTime.Today;
        public string Observacao { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string StatusReason { get; set; } = string.Empty;
        public DateTime? StatusChangedAt { get; set; }
    }
}
