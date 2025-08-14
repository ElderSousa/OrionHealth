using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V251.Group; 
using NHapi.Model.V251.Message;

using OrionHealth.Application.Interfaces;
using OrionHealth.Domain.Entities;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// A classe HapiParser é a nossa implementação concreta da interface IHL7Parser.
/// Ela usa a biblioteca NHapi para fazer todo o trabalho pesado de ler e escrever
/// mensagens no formato HL7 v2.
/// </summary>
public class HapiParser : IHL7Parser
{
    private readonly PipeParser _parser = new PipeParser();
    

    /// <summary>
    /// Gera uma mensagem de confirmação positiva (ACK) de forma robusta e corretamente formatada.
    /// </summary>
    /// <param name="originalMessage">A mensagem original recebida.</param>
    /// <returns>Uma string contendo a mensagem ACK perfeitamente formatada.</returns>
    public string CreateAck(string originalMessage)
    {
        try
        {
            var original = _parser.Parse(originalMessage);
            var terserOriginal = new Terser(original);
            var ack = new ACK();

            var msh = ack.MSH;

            msh.FieldSeparator.Value = "|";
            msh.EncodingCharacters.Value = "^~\\&";

            msh.MessageType.MessageCode.Value = "ACK";
            msh.SendingApplication.NamespaceID.Value = terserOriginal.Get("MSH-5-1");
            msh.SendingFacility.NamespaceID.Value = terserOriginal.Get("MSH-6-1");
            msh.ReceivingApplication.NamespaceID.Value = terserOriginal.Get("MSH-3-1");
            msh.ReceivingFacility.NamespaceID.Value = terserOriginal.Get("MSH-4-1");
            msh.DateTimeOfMessage.Time.SetLongDateWithSecond(DateTime.Now);
            msh.MessageControlID.Value = Guid.NewGuid().ToString().Substring(0, 20);
            msh.ProcessingID.ProcessingID.Value = "P";
            msh.VersionID.VersionID.Value = "2.5.1";
            
            var msa = ack.MSA;
            msa.AcknowledgmentCode.Value = "AA";
            msa.MessageControlID.Value = terserOriginal.Get("MSH-10-1");

            return _parser.Encode(ack);
        }
        catch (Exception e)
        {
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||{e.Message}";
        }
    }


    /// <summary>
    /// Gera uma mensagem de confirmação negativa (NAK) de forma robusta e corretamente formatada.
    /// </summary>
    /// <param name="originalMessage">A mensagem original que recebemos e que causou o erro.</param>
    /// <param name="errorMessage">A mensagem de erro específica que descreve o problema.</param>
    /// <returns>Uma string contendo a mensagem NAK no formato HL7.</returns>
    public string CreateNak(string originalMessage, string errorMessage)
    {
        try
        {
            var original = _parser.Parse(originalMessage);
            var terserOriginal = new Terser(original);
            var nak = new ACK(); // A estrutura base de um NAK é um ACK.

            var msh = nak.MSH;

            msh.FieldSeparator.Value = "|";
            msh.EncodingCharacters.Value = "^~\\&";
            
            msh.MessageType.MessageCode.Value = "ACK";
            msh.SendingApplication.NamespaceID.Value = terserOriginal.Get("MSH-5-1");
            msh.SendingFacility.NamespaceID.Value = terserOriginal.Get("MSH-6-1");
            msh.ReceivingApplication.NamespaceID.Value = terserOriginal.Get("MSH-3-1");
            msh.ReceivingFacility.NamespaceID.Value = terserOriginal.Get("MSH-4-1");
            msh.DateTimeOfMessage.Time.SetLongDateWithSecond(DateTime.Now);
            msh.MessageControlID.Value = Guid.NewGuid().ToString().Substring(0, 20);
            msh.ProcessingID.ProcessingID.Value = "P";
            msh.VersionID.VersionID.Value = "2.5.1";

            var msa = nak.MSA;
            msa.AcknowledgmentCode.Value = "AE";
            msa.MessageControlID.Value = terserOriginal.Get("MSH-10-1");
            msa.TextMessage.Value = errorMessage;

            return _parser.Encode(nak);
        }
        catch (Exception)
        {
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||Mensagem HL7 mal formada.";
        }
    }

    /// <summary>
    /// Faz o parse de uma mensagem ORU_R01 e extrai os dados do paciente e dos resultados.
    /// Este método usa parâmetros 'out', o que significa que ele vai "preencher" as variáveis
    /// 'patient' e 'results' que forem passadas para ele, em vez de retornar um único valor.
    /// </summary>
    /// <param name="hl7Message">A mensagem ORU_R01 em formato de texto.</param>
    /// <param name="patient">O objeto Paciente que será preenchido (saída).</param>
    /// <param name="results">A lista de Resultados que será preenchida (saída).</param>
    public void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results)
    {
        var oruMessage = _parser.Parse(hl7Message) as ORU_R01
            ?? throw new ArgumentException("A mensagem fornecida não é um ORU_R01 válido.");

        var pidSegment = oruMessage.GetPATIENT_RESULT().PATIENT.PID;

        patient = new Patient
        {
            MedicalRecordNumber = pidSegment.GetPatientIdentifierList().FirstOrDefault()?.IDNumber.Value ?? string.Empty,

            FullName = pidSegment.GetPatientName(0).FamilyName.Surname.Value + " " + pidSegment.GetPatientName(0).GivenName.Value,

            DateOfBirth = ParseHl7Date(pidSegment.DateTimeOfBirth.Time.Value)
        };

        results = new List<ObservationResult>();

        for (int i = 0; i < oruMessage.GetPATIENT_RESULT().ORDER_OBSERVATIONRepetitionsUsed; i++)
        {
            var orderObservation = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION(i);
            
            for (int j = 0; j < orderObservation.OBSERVATIONRepetitionsUsed; j++)
            {
                var observation = orderObservation.GetOBSERVATION(j);
                var obx = observation.OBX;

                results.Add(new ObservationResult
                {
                    ObservationId = obx.ObservationIdentifier.Identifier.Value,
                    ObservationText = obx.ObservationIdentifier.Text.Value,
                    ObservationValue = obx.GetObservationValue(0).Data.ToString() ?? string.Empty,
                    Units = obx.Units.Text.Value,
                    ObservationDateTime = ParseHl7Date(obx.DateTimeOfTheObservation.Time.Value),
                    Status = obx.ObservationResultStatus.Value
                });
            }
        }
    }

    /// <summary>
    /// Converte uma data no formato HL7 (string) para um objeto DateTime? do C#.
    /// </summary>
    private DateTime? ParseHl7Date(string? hl7Date)
    {
        if (string.IsNullOrEmpty(hl7Date))
        {
            return null;
        }

        if (DateTime.TryParseExact(hl7Date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ||
            DateTime.TryParseExact(hl7Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return result;
        }

        return null;
    }
}