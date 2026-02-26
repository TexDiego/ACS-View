using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidGroupMap : ClassMap<CidGroup>
    {
        public CidGroupMap()
        {
            Map(m => m.Code).Convert(row =>
            {
                var initial = row.Row.GetField("CATINIC");
                var final = row.Row.GetField("CATFIM");

                return $"{initial}-{final}";
            });
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}