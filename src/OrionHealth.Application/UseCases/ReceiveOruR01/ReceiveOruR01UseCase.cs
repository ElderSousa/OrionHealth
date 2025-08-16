namespace OrionHealth.Application.UseCases.ReceiveOruR01;

using OrionHealth.Application.Interfaces;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using System.Threading.Tasks;
using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;
using Microsoft.Extensions.Logging;
using NHapi.Base.Parser;
using NHapi.Base.Util;

public class ReceiveOruR01UseCase : IReceiveOruR01UseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHL7Parser _hl7Parser;
    private readonly ILogger<ReceiveOruR01UseCase> _logger;

    private PipeParser _piperParser = new PipeParser();

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

            if (VerifyTypeHL7(hl7Message) == "ORU^R01")
            {
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
            }
            else if (VerifyTypeHL7(hl7Message) == "ADT^A08")
            {
                _hl7Parser.ParseAdtA08(hl7Message, out var patient);
                _logger.LogInformation("Mensagem HL7 Parseada. Paciente: MRN: {MRN}, Nome: {FullName}", patient.MedicalRecordNumber, patient.FullName);

                var existingPatient = await _unitOfWork.Patients.FindByMrnAsync(patient.MedicalRecordNumber);

                if (existingPatient == null)
                {
                    _logger.LogWarning("Tentativa de atualização para paciente com MRN {MRN} não encontrado.", patient.MedicalRecordNumber);
                    string nakMessage = _hl7Parser.CreateNak(hl7Message, "Patient not found");
                    return new Hl7ProcessingResult(false, nakMessage);
                }
                     
                _logger.LogInformation("Paciente com MRN {MRN} encontrado (ID: {PatientId}). Atualizando dados.", existingPatient.MedicalRecordNumber, existingPatient.Id);
                existingPatient.FullName = patient.FullName;
                existingPatient.DateOfBirth = patient.DateOfBirth;
                patient = existingPatient;         
            }
            else
            {
                _logger.LogWarning("Tipo de mensagem HL7 não suportado: {hl7Message}", hl7Message);
                string nakMessage = _hl7Parser.CreateNak(hl7Message, "Tipo de mensagem inválida");
                return new Hl7ProcessingResult(false, nakMessage);
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

    private string VerifyTypeHL7(string HL7Message)
    {
        var HL7Original = _piperParser.Parse(HL7Message);
        var terserHL7 = new Terser(HL7Original);

        return terserHL7.Get("MSH-9-1"); 
        
    }
}
