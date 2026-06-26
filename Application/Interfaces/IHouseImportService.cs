using ACS_View.Application.DTOs;

namespace ACS_View.Application.Interfaces;

public interface IHouseImportService
{
    Task<HouseImportResultDto> ImportAsync(
        Stream fileStream,
        HouseImportColumnMapDto columnMap,
        IProgress<ImportProgressDto>? progress = null,
        CancellationToken cancellationToken = default);
}
