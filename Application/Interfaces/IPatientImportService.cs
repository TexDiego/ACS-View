using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces
{
    public interface IPatientImportService
    {
        Task<PatientImportResultDto> ImportAsync(Stream fileStream, PatientImportColumnMapDto columnMap);
    }
}
