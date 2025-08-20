using Microsoft.EntityFrameworkCore;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Domain.Entities;
using OrionHealth.Infrastructure.Persistence.Context;
using Dapper;
using System.Data;

namespace OrionHealth.Infrastructure.Persistence.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnection _dbConnection;

    public PatientRepository(ApplicationDbContext context, IDbConnection dbConnection)
    {
        _context = context;
        _dbConnection = dbConnection;
    }

    public void Add(Patient patient)
    {
        _context.Patients.Add(patient);
    }

    public async Task<Patient?> FindByMrnAsync(string mrn)
    {
        var sql = "SELECT * FROM PATIENTS WHERE MEDICAL_RECORD_NUMBER = :mrn";

        return await _dbConnection.QueryFirstOrDefaultAsync<Patient>(sql, new { mrn });
    }

    public void Update(Patient patient)
    {
        _context.Patients.Update(patient);
    }
}