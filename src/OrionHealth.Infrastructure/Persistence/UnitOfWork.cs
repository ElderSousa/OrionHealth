using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Infrastructure.Persistence.Context;
using OrionHealth.Infrastructure.Persistence.Repositories;
using System.Data; 

namespace OrionHealth.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnection _dbConnection;

    private IPatientRepository? _patientRepository;
    private IObservationResultRepository? _observationResultRepository;

    public UnitOfWork(ApplicationDbContext context, IDbConnection dbConnection)
    {
        _context = context;
        _dbConnection = dbConnection;
    }

    public IPatientRepository Patients =>
        _patientRepository ??= new PatientRepository(_context, _dbConnection);

    public IObservationResultRepository ObservationResults =>
        _observationResultRepository ??= new ObservationResultRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _dbConnection.Dispose();
        GC.SuppressFinalize(this);
    }
}