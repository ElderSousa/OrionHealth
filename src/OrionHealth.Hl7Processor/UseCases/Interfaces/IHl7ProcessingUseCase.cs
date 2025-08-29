using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;

public interface IHl7ProcessingUseCase
{
    Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message);
}