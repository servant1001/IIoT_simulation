using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Domain.Entities;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class CommunicationRecordRepository(FaultDiagnosisDbContext dbContext) : ICommunicationRecordRepository
{
    public Task AddAsync(CommunicationRecord record, CancellationToken cancellationToken) =>
        dbContext.CommunicationRecords.AddAsync(record, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
