using Microsoft.EntityFrameworkCore;
using OrionHealth.Domain.Entities;

namespace OrionHealth.Infrastructure.Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<ObservationResult> ObservationResults { get; set; }


    /// <summary>
    /// Este método é chamado pelo Entity Framework quando ele está construindo o modelo
    /// do banco de dados pela primeira vez. É aqui que definimos todas as regras de
    /// mapeamento entre nossas classes C# e as tabelas do Oracle.
    /// </summary>
    /// <param name="modelBuilder">O "construtor de modelo", a ferramenta que usamos para definir as regras.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("ORIONHEALTH");

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("PATIENTS");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("ID")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.MedicalRecordNumber)
                .HasColumnName("MEDICAL_RECORD_NUMBER")
                .IsRequired();

            entity.HasIndex(p => p.MedicalRecordNumber).IsUnique();
            
            entity.Property(p => p.FullName)
                .HasColumnName("FULL_NAME")
                .IsRequired();

            entity.Property(p => p.DateOfBirth)
                .HasColumnName("DATE_OF_BIRTH");
        });

        modelBuilder.Entity<ObservationResult>(entity =>
        {
            entity.ToTable("OBSERVATION_RESULTS");

            entity.HasKey(or => or.Id);

            entity.Property(or => or.Id)
                .HasColumnName("ID")
                .ValueGeneratedOnAdd();
            
            entity.Property(or => or.PatientId)
                .HasColumnName("PATIENT_ID")
                .IsRequired();

            entity.Property(or => or.ObservationId).HasColumnName("OBSERVATION_ID").IsRequired();
            entity.Property(or => or.ObservationValue).HasColumnName("OBSERVATION_VALUE").IsRequired();
            entity.Property(or => or.Status).HasColumnName("STATUS").IsRequired();
            
            entity.Property(or => or.ObservationText).HasColumnName("OBSERVATION_TEXT");
            entity.Property(or => or.Units).HasColumnName("UNITS");
            entity.Property(or => or.ObservationDateTime).HasColumnName("OBSERVATION_DATE_TIME");
            
            entity
                .HasOne<Patient>()
                .WithMany()
                .HasForeignKey(or => or.PatientId);
        });
    }
}