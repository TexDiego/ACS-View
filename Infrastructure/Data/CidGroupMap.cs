using ACS_View.Domain.Entities.Health;
using CsvHelper.Configuration;

namespace ACS_View.Infrastructure.Data
{
    internal sealed class CidGroupMap : ClassMap<CidGroup>
    {
        public CidGroupMap()
        {
            // Keep original code as range (e.g. "A00-A09")
            Map(m => m.Code).Convert(row =>
            {
                var initial = row.Row.GetField("CATINIC");
                var final = row.Row.GetField("CATFIM");

                return $"{initial}-{final}";
            });
            // Also store initial and final parts separately on the entity
            Map(m => m.InitialCode).Name("CATINIC");
            Map(m => m.FinalCode).Name("CATFIM");
            Map(m => m.Description).Name("DESCRICAO");
        }
    }
}