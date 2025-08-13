namespace OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;

public interface IReceiveOruR01UseCase
{
    public record Hl7ProcessingResult(bool IsSuccess, string AckNackMessage);
    
    Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message);

}