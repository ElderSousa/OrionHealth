using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces.Persistence;

public interface IObservationResultRepository
{
    void Add(ObservationResult observationResult);
}