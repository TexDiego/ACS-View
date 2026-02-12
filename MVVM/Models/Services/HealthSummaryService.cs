using ACS_View.MVVM.Models.DTOs;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;

namespace ACS_View.MVVM.Models.Services
{
    public class HealthSummaryService : IHealthSummaryService
    {
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();
        private readonly IHouseService _houseService = App.ServiceProvider.GetRequiredService<IHouseService>();

        public async Task<HealthSummary> GetHealthSummaryAsync()
        {
            return new HealthSummary
            {
                TotalHouses = await _houseService.GetTotalCountAsync(),
                TotalGestantes = await _healthRecordService.GetConditionCountAsync(r => r.IsPregnant),
                TotalDiabeticos = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes),
                TotalHipertensos = await _healthRecordService.GetConditionCountAsync(r => r.HasHypertension),
                TotalDiabetesHipertensao = await _healthRecordService.GetConditionCountAsync(r => r.HasDiabetes && r.HasHypertension),
                TotalTuberculose = await _healthRecordService.GetConditionCountAsync(r => r.HasTuberculosis),
                TotalHanseniase = await _healthRecordService.GetConditionCountAsync(r => r.HasLeprosy),
                TotalAcamados = await _healthRecordService.GetConditionCountAsync(r => r.IsBedridden),
                TotalDomiciliados = await _healthRecordService.GetConditionCountAsync(r => r.IsHomebound),
                TotalMenores6Anos = await _healthRecordService.GetYoungerCountAsync(),
                TotalMental = await _healthRecordService.GetConditionCountAsync(r => r.HasMentalIllness),
                TotalFumante = await _healthRecordService.GetConditionCountAsync(r => r.IsSmoker),
                TotalAlcoolatra = await _healthRecordService.GetConditionCountAsync(r => r.IsAlcoholic),
                TotalBolsaFamilia = await _healthRecordService.GetConditionCountAsync(r => r.BolsaFamilia),
                TotalDeficiente = await _healthRecordService.GetConditionCountAsync(r => r.HasDisabilities),
                TotalHeartDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasHeartDesease),
                TotalKidneyDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasKidneyDesease),
                TotalLungDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasLungsDesease),
                TotalLiverDesease = await _healthRecordService.GetConditionCountAsync(r => r.HasLiverDesease),
                TotalNeurodivergents = await _healthRecordService.GetConditionCountAsync(r => r.IsNeurodivergent),
                TotalDrugsAddicted = await _healthRecordService.GetConditionCountAsync(r => r.IsDrugAddicted),
                TotalHIV = await _healthRecordService.GetConditionCountAsync(r => r.HasHIV),
                TotalCancer = await _healthRecordService.GetConditionCountAsync(r => r.HasCancer),
                TotalOld = await _healthRecordService.GetElderCountAsync(),
                Total = await _healthRecordService.GetTotalCountAsync(),
                NoResidence = await _healthRecordService.GetConditionCountAsync(r => r.HouseId == 0)
            };
        }
    }
}