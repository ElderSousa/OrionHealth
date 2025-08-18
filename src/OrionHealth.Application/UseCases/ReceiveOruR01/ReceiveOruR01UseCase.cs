namespace OrionHealth.Application.UseCases.ReceiveOruR01;

using OrionHealth.Application.Interfaces;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using System.Threading.Tasks;
using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;
using Microsoft.Extensions.Logging;

public class ReceiveOruR01UseCase : IReceiveOruR01UseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHL7Parser _hl7Parser;
    private readonly ILogger<ReceiveOruR01UseCase> _logger;

    public ReceiveOruR01UseCase(IUnitOfWork unitOfWork, IHL7Parser hl7Parser, ILogger<ReceiveOruR01UseCase> logger) // Logger injetado
    {
        _unitOfWork = unitOfWork;
        _hl7Parser = hl7Parser;
        _logger = logger; 
    }

    public async Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message)
    {
        try
        {
            _logger.LogInformation("Iniciando processamento da mensagem HL7.");

            _hl7Parser.ParseOruR01(hl7Message, out var patient, out var results);
            _logger.LogInformation("Mensagem HL7 parseada. Paciente MRN: {MRN}, Nome: {FullName}. Total de resultados: {ResultCount}",
                                   patient.MedicalRecordNumber, patient.FullName, results.Count);

            var existingPatient = await _unitOfWork.Patients.FindByMrnAsync(patient.MedicalRecordNumber);

            if (existingPatient is null)
            {
                _logger.LogInformation("Paciente com MRN {MRN} não encontrado. Adicionando novo paciente.", patient.MedicalRecordNumber);
                _unitOfWork.Patients.Add(patient);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Novo paciente salvo com ID: {PatientId}", patient.Id);
            }
            else
            {
                _logger.LogInformation("Paciente com MRN {MRN} encontrado (ID: {PatientId}). Atualizando dados.", existingPatient.MedicalRecordNumber, existingPatient.Id);
                existingPatient.FullName = patient.FullName;
                patient = existingPatient;
            }

            if (results.Any())
            {
                _logger.LogInformation("Associando {ResultCount} resultados ao paciente ID: {PatientId}", results.Count, patient.Id);
                foreach (var result in results)
                {
                    result.PatientId = patient.Id;
                    _unitOfWork.ObservationResults.Add(result);
                    _logger.LogDebug("Adicionado resultado: ID={ObsId}, Valor={ObsValue}, PacienteID={PatientId}",
                                     result.ObservationId, result.ObservationValue, result.PatientId);
                }
            }
            else
            {
                _logger.LogWarning("Nenhum resultado de observação encontrado na mensagem HL7 para o paciente MRN: {MRN}", patient.MedicalRecordNumber);
            }


            var savedChanges = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("{SavedChanges} alterações salvas no banco de dados (incluindo resultados de observação).", savedChanges);

            string ackMessage = _hl7Parser.CreateAck(hl7Message);
            _logger.LogInformation("Processamento concluído com sucesso. Enviando ACK.");
            return new Hl7ProcessingResult(true, ackMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante o processamento da mensagem HL7. Mensagem: {ErrorMessage}", ex.Message);
            string nakMessage = _hl7Parser.CreateNak(hl7Message, ex.Message);
            return new Hl7ProcessingResult(false, nakMessage);
        }
    }
}
