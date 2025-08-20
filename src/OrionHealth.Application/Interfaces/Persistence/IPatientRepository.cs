using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces.Persistence;

public interface IPatientRepository
{
    Task<Patient?> FindByMrnAsync(string mrn);

    void Add(Patient patient);
    void Update(Patient patient);
}
