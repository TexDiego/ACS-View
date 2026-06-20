using ACS_View.UseCases.DTOs;

namespace ACS_View.Domain.Interfaces
{
    public interface IPatientImportService
    {
        Task<PatientImportResultDto> ImportAsync(Stream fileStream, PatientImportColumnMapDto columnMap);
    }
}
