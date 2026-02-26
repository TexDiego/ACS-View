using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidCategoriesMap : ClassMap<CidCategory>
    {
        public CidCategoriesMap()
        {
            Map(m => m.Code).Name("CAT");
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}