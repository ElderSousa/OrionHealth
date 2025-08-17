using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V251.Message;

using OrionHealth.Application.Interfaces;
using OrionHealth.Domain.Entities;

using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Text;

namespace OrionHealth.Infrastructure.HL7;

/// <summary>
/// A classe HapiParser é a nossa implementação concreta da interface IHL7Parser.
/// Ela é responsável por toda a lógica de parsing (leitura) de mensagens HL7 ORU^R01
/// e pela criação de mensagens de resposta (ACK/NAK), utilizando a biblioteca NHapi.
/// </summary>
public class HapiParser : IHL7Parser
{
    private readonly PipeParser _parser = new PipeParser();
    private readonly ILogger<HapiParser> _logger;

    public HapiParser(ILogger<HapiParser> logger)
    {
        _logger = logger; // Atribui a instância do logger injetado ao campo privado para uso posterior.

    }

    /// <summary>
    /// Gera uma mensagem de confirmação positiva (ACK - Acknowledgment) no formato HL7 v2.5.1.
    /// Esta função constrói a mensagem ACK de forma robusta, utilizando as propriedades fortemente tipadas
    /// dos objetos do NHapi, o que é mais seguro e menos propenso a erros do que manipulação via Terser para criação.
    /// </summary>
    /// <param name="originalMessage">A mensagem HL7 original recebida, da qual informações como o ID de controle serão copiadas.</param>
    /// <returns>Uma string contendo a mensagem ACK formatada em HL7.</returns>
    public string CreateAck(string originalMessage)
    {
        try
        {
            var original = _parser.Parse(originalMessage);
            var terserOriginal = new Terser(original);
            var ack = new ACK();

            var msh = ack.MSH;
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
            _logger.LogError(e, "HapiParser: Erro ao criar ACK para mensagem original: {OriginalMessage}", originalMessage);
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||{e.Message}";
        }
    }

    /// <summary>
    /// Gera uma mensagem de confirmação negativa (NAK - Negative Acknowledgment) no formato HL7 v2.5.1.
    /// </summary>
    /// <param name="originalMessage">A mensagem HL7 original recebida.</param>
    /// <param name="errorMessage">A mensagem de erro detalhada a ser incluída no NAK.</param>
    /// <returns>Uma string contendo a mensagem NAK formatada em HL7.</returns>
    public string CreateNak(string originalMessage, string errorMessage)
    {
        try
        {
            var original = _parser.Parse(originalMessage);
            var terserOriginal = new Terser(original);
            var originalMessageControlId = terserOriginal.Get("MSH-10-1");

            var nak = new ACK(); 
            var terserNak = new Terser(nak);

            terserNak.Set("MSH-3-1", terserOriginal.Get("MSH-5-1"));
            terserNak.Set("MSH-4-1", terserOriginal.Get("MSH-6-1"));
            terserNak.Set("MSH-5-1", terserOriginal.Get("MSH-3-1"));
            terserNak.Set("MSH-6-1", terserOriginal.Get("MSH-4-1"));
            terserNak.Set("MSH-7-1", DateTime.Now.ToString("yyyyMMddHHmmss"));
            terserNak.Set("MSH-9-1", "ACK");
            terserNak.Set("MSA-10-1", Guid.NewGuid().ToString("N").Substring(0, 20));
            terserNak.Set("MSH-11-1", "P");
            terserNak.Set("MSH-12-1", "2.5.1");

            terserNak.Set("MSA-1-1", "AE");
            terserNak.Set("MSA-2-1", originalMessageControlId);
            terserNak.Set("MSA-3-1", errorMessage);

            return _parser.Encode(nak);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HapiParser: Erro ao criar NAK para mensagem original: {OriginalMessage}. Fallback para NAK genérico.", originalMessage);
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||Mensagem HL7 mal formada ou erro interno ao gerar NAK.";
        }
    }
    /// <summary>
    /// Faz o parse de uma mensagem HL7 ADT^A01 e extrai os dados do paciente.
    /// Este método utiliza parâmetro 'out' para retornar valor (o paciente).
    /// </summary>
    /// <param name="hl7Message">A mensagem ADT_A01 em formato de texto (string).</param>
    /// <param name="patient">Parâmetro de saída: O objeto Patient preenchido com os dados da mensagem.</param>
    public void ParseAdtA08(string hl7Message, out Patient patient)
    {
        _logger.LogInformation("HapiParser: Iniciando parse de ADT_A08");
        var adtMessege = _parser.Parse(hl7Message) as ADT_A01;
        var pidSegment = adtMessege?.PID;

        patient = new Patient
        {
            MedicalRecordNumber = pidSegment?.GetPatientIdentifierList().FirstOrDefault()?.IDNumber.Value ?? string.Empty,
            FullName = pidSegment?.GetPatientName(0).FamilyName.Surname.Value + " " + pidSegment?.GetPatientName(0).GivenName.Value,
            DateOfBirth = ParseHl7Date(pidSegment?.DateTimeOfBirth.Time.Value)
        };

    }

    /// <summary>
    /// Faz o parse de uma mensagem HL7 ORU^R01 e extrai os dados do paciente e de seus resultados de observação.
    /// Este método utiliza parâmetros 'out' para retornar múltiplos valores (o paciente e a lista de resultados).
    /// </summary>
    /// <param name="hl7Message">A mensagem ORU_R01 em formato de texto (string).</param>
    /// <param name="patient">Parâmetro de saída: O objeto Patient preenchido com os dados da mensagem.</param>
    /// <param name="results">Parâmetro de saída: A lista de objetos ObservationResult preenchida com os dados dos exames.</param>
    public void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results)
    {
        try
        {
            _logger.LogInformation("HapiParser: Iniciando parse de ORU_R01.");

            var oruMessage = _parser.Parse(hl7Message) as ORU_R01
                ?? throw new ArgumentException("A mensagem fornecida não é um ORU_R01 válido.");

            var pidSegment = oruMessage.GetPATIENT_RESULT().PATIENT.PID;

            patient = new Patient
            {
                MedicalRecordNumber = pidSegment.GetPatientIdentifierList().FirstOrDefault()?.IDNumber.Value ?? string.Empty,

                FullName = pidSegment.GetPatientName(0).FamilyName.Surname.Value + " " + pidSegment.GetPatientName(0).GivenName.Value,

                DateOfBirth = ParseHl7Date(pidSegment.DateTimeOfBirth.Time.Value)
            };
            _logger.LogInformation("HapiParser: Paciente extraído: MRN={MRN}, Nome={FullName}", patient.MedicalRecordNumber, patient.FullName);

            results = new List<ObservationResult>();

            int orderObservationCount = oruMessage.GetPATIENT_RESULT().ORDER_OBSERVATIONRepetitionsUsed;
            _logger.LogInformation("HapiParser: Total de ORDER_OBSERVATIONs encontrados: {Count}", orderObservationCount);

            for (int i = 0; i < orderObservationCount; i++)
            {
                var orderObservation = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION(i);
                _logger.LogInformation("HapiParser: Processando ORDER_OBSERVATION[{Index}]", i);

                int observationCount = orderObservation.OBSERVATIONRepetitionsUsed;
                _logger.LogInformation("HapiParser: Total de OBSERVATIONs encontrados em ORDER_OBSERVATION[{Index}]: {Count}", i, observationCount);

                for (int j = 0; j < observationCount; j++)
                {
                    var observation = orderObservation.GetOBSERVATION(j);
                    var obx = observation.OBX;
                    _logger.LogInformation("HapiParser: Extraindo OBX[{Index}] em ORDER_OBSERVATION[{ParentIndex}]", j, i);

                    results.Add(new ObservationResult
                    {
                        ObservationId = obx.ObservationIdentifier.Identifier.Value,
                        ObservationText = obx.ObservationIdentifier.Text.Value,
                        ObservationValue = obx.GetObservationValue(0).Data.ToString() ?? string.Empty,
                        Units = obx.Units.Text.Value,
                        ObservationDateTime = ParseHl7Date(obx.DateTimeOfTheObservation.Time.Value),
                        Status = obx.ObservationResultStatus.Value
                    });
                    _logger.LogDebug("HapiParser: OBX adicionado: ID={ObsId}, Valor={ObsValue}", obx.ObservationIdentifier.Identifier.Value, obx.GetObservationValue(0).Data.ToString());
                }
            }
            _logger.LogInformation("HapiParser: Parse de ORU_R01 concluído. Total de resultados extraídos: {Count}", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HapiParser: Erro durante o parse da mensagem HL7: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Converte uma string de data/hora no formato HL7 (ex: "yyyyMMddHHmmss" ou "yyyyMMdd")
    /// para um objeto DateTime? (nullable DateTime) do C#.
    /// </summary>
    /// <param name="hl7Date">A string de data/hora no formato HL7.</param>
    /// <returns>Um objeto DateTime? se o parse for bem-sucedido, caso contrário, null.</returns>
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
