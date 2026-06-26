namespace ACS_View.Application.DTOs
{
    public sealed class ImportProgressDto
    {
        public int ProcessedItems { get; init; }
        public int TotalItems { get; init; }
        public string CurrentStep { get; init; } = string.Empty;

        public double Progress => TotalItems <= 0
            ? 0
            : Math.Clamp((double)ProcessedItems / TotalItems, 0, 1);
    }
}