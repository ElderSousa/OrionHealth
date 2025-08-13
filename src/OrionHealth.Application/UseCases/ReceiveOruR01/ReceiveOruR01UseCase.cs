namespace OrionHealth.Application.UseCases.ReceiveOruR01;

using OrionHealth.Application.Interfaces;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using System.Threading.Tasks;
using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;

public class ReceiveOruR01UseCase : IReceiveOruR01UseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHL7Parser _hl7Parser;

    public ReceiveOruR01UseCase(IUnitOfWork unitOfWork, IHL7Parser hl7Parser)
    {
        _unitOfWork = unitOfWork;
        _hl7Parser = hl7Parser;
    }

   public async Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message)
    {
        try
        {
            _hl7Parser.ParseOruR01(hl7Message, out var patient, out var results);

            var existingPatient = await _unitOfWork.Patients.FindByMrnAsync(patient.MedicalRecordNumber);

            if (existingPatient is null)
            {
                _unitOfWork.Patients.Add(patient);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                existingPatient.FullName = patient.FullName;
                patient = existingPatient;
            }

            foreach (var result in results)
            {
                result.PatientId = patient.Id;
                _unitOfWork.ObservationResults.Add(result);
            }

            await _unitOfWork.SaveChangesAsync();

            string ackMessage = _hl7Parser.CreateAck(hl7Message);
            return new Hl7ProcessingResult(true, ackMessage);
        }
        catch (Exception ex)
        {
            string nakMessage = _hl7Parser.CreateNak(hl7Message, ex.Message);
            return new Hl7ProcessingResult(false, nakMessage);
        }
    }
}