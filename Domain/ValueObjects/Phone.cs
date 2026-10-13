using CommunityToolkit.Mvvm.ComponentModel;

namespace ACS_View.Domain.ValueObjects
{
    public partial class Phone : ObservableObject
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        private string number = string.Empty;
        private bool isPrimary;
        public string Number { get => number; set => SetProperty(ref number, value); }
        public bool IsPrimary { get => isPrimary; set => SetProperty(ref isPrimary, value); }
        public static string Digits(string? value) => new((value ?? string.Empty).Where(c => c is >= '0' and <= '9').ToArray());
        public static string Format(string? value)
        {
            var digits = Digits(value);
            return digits.Length switch
            {
                11 => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
                10 => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
                _ => value ?? string.Empty
            };
        }
    }
}
