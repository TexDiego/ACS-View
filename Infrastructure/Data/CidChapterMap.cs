using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidChapterMap : ClassMap<CidChapter>
    {
        public CidChapterMap()
        {
            Map(m => m.Code).Name("NUMCAP");
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}