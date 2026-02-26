using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidSubcategoriesMap : ClassMap<CidSubcategory>
    {
        public CidSubcategoriesMap()
        {
            Map(m => m.Code).Name("SUBCAT");
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}