namespace OrionHealth.Domain.Entities;

public class ObservationResult
{
    public long Id { get; set; }

    public long PatientId { get; set; }

    public string ObservationId { get; set; } = null!;

    public string? ObservationText { get; set; }

    public string ObservationValue { get; set; } = null!;

    public string? Units { get; set; }

    public DateTime? ObservationDateTime { get; set; }

    public string Status { get; set; } = null!;

}