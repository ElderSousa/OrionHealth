using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces;

public interface IHL7Parser
{
    void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results);

    void ParseAdtA08(string hl7Message, out Patient patient);

    string CreateAck(string originalMessage);

    string CreateNak(string originalMessage, string errorMessage);
}