using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Domain.Entities;
using OrionHealth.Infrastructure.Persistence.Context;

namespace OrionHealth.Infrastructure.Persistence.Repositories;

public class ObservationResultRepository : IObservationResultRepository
{
    private readonly ApplicationDbContext _context;

    public ObservationResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(ObservationResult observationResult)
    {
        _context.ObservationResults.Add(observationResult);
    }
}