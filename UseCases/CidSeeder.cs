using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using ACS_View.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace ACS_View.UseCases
{
    internal class CidSeeder(ICidRepository repository) : ICidSeeder
    {
        private readonly ICidRepository _repository = repository;

        public async Task SeedAsync()
        {
            if (await _repository.AnyAsync())
                return;

            var chapters = CidSeeder.LoadFromCsv<CidChapter>("cid_chapters.csv");
            var groups = CidSeeder.LoadFromCsv<CidGroup>("cid_groups.csv");
            var categories = CidSeeder.LoadFromCsv<CidCategory>("cid_categories.csv");
            var subcategories = CidSeeder.LoadFromCsv<CidSubcategory>("cid_subcategories.csv");

            foreach (var sub in subcategories)
            {
                sub.CategoryCode = sub.Code[..3];
            }
            foreach (var category in categories)
            {
                var group = groups.FirstOrDefault(g =>
                {
                    var parts = g.Code.Split('-');
                    return string.Compare(category.Code, parts[0]) >= 0 &&
                           string.Compare(category.Code, parts[1]) <= 0;
                });

                if (group != null)
                    category.GroupCode = group.Code;
            }
            foreach (var group in groups)
            {
                var initial = group.Code.Split('-')[0];

                var chapter = chapters.FirstOrDefault(c =>
                    string.Compare(initial, c.InitialCode) >= 0 &&
                    string.Compare(initial, c.FinalCode) <= 0
                );

                if (chapter != null)
                    group.ChapterCode = chapter.Code;
            }

            await _repository.InsertChaptersAsync(chapters);
            await _repository.InsertGroupsAsync(groups);
            await _repository.InsertCategoriesAsync(categories);
            await _repository.InsertSubcategoriesAsync(subcategories);
        }

        private static List<T> LoadFromCsv<T>(string fileName)
        {
            var assembly = typeof(CidSeeder).Assembly;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
            };

            using var stream = assembly.GetManifestResourceStream($"ACS_View.Infrastructure.Data.Seed.{fileName}");
            using var reader = new StreamReader(stream, Encoding.GetEncoding("ISO-8859-1"));
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<CidChapterMap>();
            csv.Context.RegisterClassMap<CidGroupMap>();
            csv.Context.RegisterClassMap<CidCategoriesMap>();
            csv.Context.RegisterClassMap<CidSubcategoriesMap>();

            return [.. csv.GetRecords<T>()];
        }
    }
}