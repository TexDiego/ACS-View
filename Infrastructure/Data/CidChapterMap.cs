using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidChapterMap : ClassMap<CidChapter>
    {
        public CidChapterMap()
        {
            // NUMCAP contains the chapter number (1..21) and is used as the chapter code
            Map(m => m.Code).Name("NUMCAP");
            // Map the initial and final category codes for range comparisons (e.g. "A00")
            Map(m => m.InitialCode).Name("CATINIC");
            Map(m => m.FinalCode).Name("CATFIM");
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}