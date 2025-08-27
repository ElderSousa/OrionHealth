using NHapi.Base.Parser;
using NHapi.Base.Util;
using OrionHealth.Application.Interfaces;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;

namespace OrionHealth.Hl7Processor
{
    public class Hl7ProcessingUseCase : IReceiveOruR01UseCase
    {
        private readonly ILogger<Hl7ProcessingUseCase> _logger;
        private readonly IHL7Parser _hl7Parser;
        private readonly PipeParser _pipeParser = new PipeParser();

        public Hl7ProcessingUseCase(IHL7Parser hl7Parser, ILogger<Hl7ProcessingUseCase> logger)
        {
            _hl7Parser = hl7Parser;
            _logger = logger;
        }

        public Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message)
        {
            try
            {
                _logger.LogInformation("Processando mensagem HL7 no microserviço.");

                string msgType = VerifyTypeHL7(hl7Message);

                if (msgType == "ORU^R01")
                {
                    _hl7Parser.ParseOruR01(hl7Message, out var patient, out var results);
                    _logger.LogInformation("Mensagem HL7 parseada. Paciente MRN: {MRN}, Resultados: {ResultCount}", patient.MedicalRecordNumber, results.Count);
                }
                else if (msgType == "ADT^A08")
                {
                    _hl7Parser.ParseAdtA08(hl7Message, out var patient);
                    _logger.LogInformation("Mensagem ADT^A08 parseada. Paciente MRN: {MRN}", patient.MedicalRecordNumber);
                }
                else
                {
                    _logger.LogWarning("Tipo de mensagem não suportado: {Type}", msgType);
                    string nakMessage = _hl7Parser.CreateNak(hl7Message, "Tipo de mensagem inválida");
                    return Task.FromResult(new Hl7ProcessingResult(false, nakMessage));
                }

                string ackMessage = _hl7Parser.CreateAck(hl7Message);
                return Task.FromResult(new Hl7ProcessingResult(true, ackMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar HL7");
                string nakMessage = _hl7Parser.CreateNak(hl7Message, ex.Message);
                return Task.FromResult(new Hl7ProcessingResult(false, nakMessage));
            }
        }

        private string VerifyTypeHL7(string HL7Message)
        {
            try
            {
                var parsed = _pipeParser.Parse(HL7Message);
                var terser = new Terser(parsed);
                return $"{terser.Get("MSH-9-1")}^{terser.Get("MSH-9-2")}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
