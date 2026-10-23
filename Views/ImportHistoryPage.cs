using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using System.Text.Json;

namespace ACS_View.Views;

public sealed class ImportHistoryPage : ContentPage
{
    private readonly IImportHistoryService historyService;
    private readonly CollectionView imports = new() { SelectionMode = SelectionMode.Single };

    public ImportHistoryPage(IImportHistoryService historyService)
    {
        this.historyService = historyService;
        Title = "Histórico de importações";
        imports.EmptyView = "Nenhuma importação registrada.";
        imports.ItemTemplate = new DataTemplate(() =>
        {
            var title = new Label { FontAttributes = FontAttributes.Bold };
            title.SetBinding(Label.TextProperty, nameof(ImportHistory.FileName));
            var date = new Label();
            date.SetBinding(Label.TextProperty, new Binding(nameof(ImportHistory.StartedAt), converter: new ImportDateConverter()));
            var status = new Label();
            status.SetBinding(Label.TextProperty, nameof(ImportHistory.Status));
            return new VerticalStackLayout { Padding = 16, Spacing = 6, Children = { title, date, status } };
        });
        imports.SelectionChanged += async (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is not ImportHistory entry) return;
            imports.SelectedItem = null;
            try
            {
                var report = JsonSerializer.Deserialize<PatientImportResultDto>(entry.ReportJson) ?? new();
                var summary = new Label { Text = $"{entry.FileName}\nInício: {FormatDate(entry.StartedAt)}\nFim: {FormatDate(entry.FinishedAt)}\n{entry.Status}\nImportados: {report.ImportedCount} · Mesclados: {report.MergedCount} · Ignorados: {report.IgnoredCount}" };
                // Structured outcomes are authoritative for new imports. Older reports stay readable.
                var entries = report.Entries.Select(item => (Status: item.Status, Text: item.DisplayText))
                    .Concat(report.Errors.Select(text => (Status: "Pendência", Text: "Pendência · " + text)))
                    .Concat(report.Entries.Count == 0
                        ? report.Details.Select(text => (Status: "Registro anterior", Text: "Registro anterior · " + text))
                        : Enumerable.Empty<(string Status, string Text)>()).ToArray();
                var filter = new Picker { Title = "Filtrar por resultado" };
                var statuses = new[] { "Todos", "Importado", "Mesclado", "Ignorado", "Pendência", "Vínculo ajustado", "Registro anterior" };
                filter.ItemsSource = statuses.Select(status => status == "Todos" ? $"Todos ({entries.Length})" : $"{status} ({entries.Count(item => item.Status == status)})").ToArray();
                filter.SelectedIndex = 0;
                var search = new SearchBar { Placeholder = "Buscar cadastro, linha, campo ou motivo" };
                var count = new Label();
                var lines = new CollectionView { EmptyView = "Nenhum registro para este filtro." };
                lines.ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label { Margin = new Thickness(0, 8), LineBreakMode = LineBreakMode.WordWrap };
                    label.SetBinding(Label.TextProperty, ".");
                    return label;
                });
                void ApplyFilters()
                {
                    var selected = statuses[Math.Max(0, filter.SelectedIndex)];
                    var visible = entries.Where(item => (selected == "Todos" || item.Status == selected) &&
                        (string.IsNullOrWhiteSpace(search.Text) || item.Text.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)))
                        .Select(item => item.Text).ToArray();
                    lines.ItemsSource = visible;
                    count.Text = $"{visible.Length} registros encontrados";
                }
                filter.SelectedIndexChanged += (_, _) => ApplyFilters();
                search.TextChanged += (_, _) => ApplyFilters();
                ApplyFilters();
                var layout = new Grid { Padding = 16, RowSpacing = 12, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
                layout.Add(summary, 0, 0);
                layout.Add(filter, 0, 1);
                layout.Add(search, 0, 2);
                layout.Add(count, 0, 3);
                layout.Add(lines, 0, 4);
                await Navigation.PushAsync(new ContentPage { Title = "Relatório de importação", Content = layout });
            }
            catch (Exception ex) { await DisplayAlertAsync("Histórico", $"Não foi possível abrir o relatório: {ex.Message}", "Voltar"); }
        };
        Content = imports;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { imports.ItemsSource = await historyService.GetAsync(); }
        catch (Exception ex) { await DisplayAlertAsync("Histórico", $"Não foi possível carregar: {ex.Message}", "Voltar"); }
    }

    private static string FormatDate(string value) => DateTimeOffset.TryParse(value, out var date)
        ? date.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss zzz") : value;

    private sealed class ImportDateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => FormatDate(value?.ToString() ?? "");
        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
