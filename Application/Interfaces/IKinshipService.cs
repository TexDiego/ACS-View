using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;
namespace ACS_View.Application.Interfaces;
public interface IKinshipService
{
    Task<KinshipSnapshot> LoadAsync();
    Task<List<Patient>> SearchAsync(string text, int excludeId, CancellationToken cancellationToken);
    Task<KinshipPreview> PreviewAsync(IReadOnlyList<FamilyRelationship> changes, IReadOnlyList<int> removed);
    Task SaveRegistrationAsync(Patient patient, IReadOnlyList<FamilyRelationship> changes, IReadOnlyList<int> removed);
    Task SaveAsync(IReadOnlyList<FamilyRelationship> changes, IReadOnlyList<int> removed);
}
