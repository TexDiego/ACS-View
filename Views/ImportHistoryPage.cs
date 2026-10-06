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
                var summary = new Label { Text = $"{entry.FileName}\nInício: {FormatDate(entry.StartedAt)}\nFim: {FormatDate(entry.FinishedAt)}\n{entry.Status}\nCriados: {report.ImportedCount} · Atualizados: {report.UpdatedCount} · Mesclados: {report.MergedCount} · Ignorados: {report.IgnoredCount}" };
                var entries = report.Errors.Select(t => "Pendência · " + t)
                    .Concat(report.Details.Select(t => "Registro · " + t)).ToArray();
                var search = new SearchBar { Placeholder = "Buscar pessoa, linha, campo ou pendência" };
                var lines = new CollectionView { ItemsSource = entries, EmptyView = "Nenhum registro encontrado." };
                lines.ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label { Margin = new Thickness(0, 8), LineBreakMode = LineBreakMode.WordWrap };
                    label.SetBinding(Label.TextProperty, ".");
                    return label;
                });
                search.TextChanged += (_, args) => lines.ItemsSource = string.IsNullOrWhiteSpace(args.NewTextValue)
                    ? entries : entries.Where(t => t.Contains(args.NewTextValue, StringComparison.CurrentCultureIgnoreCase)).ToArray();
                var layout = new Grid { Padding = 16, RowSpacing = 12, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
                layout.Add(summary, 0, 0);
                layout.Add(search, 0, 1);
                layout.Add(lines, 0, 2);
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
