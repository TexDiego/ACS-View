using ACS_View.MVVM.Models.Interfaces;

namespace ACS_View.MVVM.Models.Services
{
    public class HealthRecordFilterService : IHealthRecordFilterService
    {
        public List<HealthRecord> ApplyFilters(
            IEnumerable<HealthRecord> records,
            string condition,
            string search,
            string filter,
            string order)
        {
            var filtered = records;

            if (!string.IsNullOrEmpty(condition))
            {
                filtered = condition switch
                {
                    "GESTANTE" => filtered.Where(r => r.IsPregnant),
                    "DB" => filtered.Where(r => r.HasDiabetes),
                    "HAS" => filtered.Where(r => r.HasHypertension),
                    "HASDB" => filtered.Where(r => r.IsDiabetesAndHypertension),
                    "HAN" => filtered.Where(r => r.HasLeprosy),
                    "TB" => filtered.Where(r => r.HasTuberculosis),
                    "ACAMADO" => filtered.Where(r => r.IsBedridden),
                    "DOMICILIADO" => filtered.Where(r => r.IsHomebound),
                    "BOLSA" => filtered.Where(r => r.BolsaFamilia),
                    "CORACAO" => filtered.Where(r => r.HasHeartDesease),
                    "FIGADO" => filtered.Where(r => r.HasLiverDesease),
                    "RIM" => filtered.Where(r => r.HasKidneyDesease),
                    "PULMAO" => filtered.Where(r => r.HasLungsDesease),
                    "NEURO" => filtered.Where(r => r.IsNeurodivergent),
                    "HIV" => filtered.Where(r => r.HasHIV),
                    "DROGAS" => filtered.Where(r => r.IsDrugAddicted),
                    "MENOR" => filtered.Where(r => r.IsBaby),
                    "MENTAL" => filtered.Where(r => r.HasMentalIllness),
                    "FUMANTE" => filtered.Where(r => r.IsSmoker),
                    "ALCOOLATRA" => filtered.Where(r => r.IsAlcoholic),
                    "DEFICIENTE" => filtered.Where(r => r.HasDisabilities),
                    "CANCER" => filtered.Where(r => r.HasCancer),
                    "IDOSO" => filtered.Where(r => r.IsOld),
                    "NOHOME" => filtered.Where(r => r.HouseId == 0),
                    _ => filtered
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string normalized = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(r =>
                    (r.Name?.ToLowerInvariant().Contains(normalized) ?? false) ||
                    (r.SusNumber?.Contains(normalized) ?? false));
            }

            filtered = filter switch
            {
                "Nome" => order == "Crescente"
                    ? filtered.OrderBy(r => r.Name)
                    : filtered.OrderByDescending(r => r.Name),

                "Idade" => order == "Crescente"
                    ? filtered.OrderByDescending(r => r.BirthDate)
                    : filtered.OrderBy(r => r.BirthDate),

                _ => filtered
            };

            return filtered.ToList();
        }
    }
}