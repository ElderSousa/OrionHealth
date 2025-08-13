namespace OrionHealth.Application.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    IPatientRepository Patients { get; }
    IObservationResultRepository ObservationResults { get; }

    Task<int> SaveChangesAsync();
}