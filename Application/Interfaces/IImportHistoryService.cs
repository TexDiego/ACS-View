using ACS_View.Domain.Entities;

namespace ACS_View.Application.Interfaces;

public interface IImportHistoryService
{
    Task SaveAsync(ImportHistory history);
    Task<List<ImportHistory>> GetAsync();
}
