namespace IIoT.FaultDiagnosis.Application.Experiments;

public interface IExperimentService
{
    Task<ExperimentDto> CreateAsync(CreateExperimentCommand command, CancellationToken cancellationToken);
    Task<ExperimentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExperimentRunDto>?> GetRunsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunicationRecordDto>?> GetRecordsAsync(Guid id, CancellationToken cancellationToken);
    Task<ExperimentExport?> ExportAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> StartAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> StopAsync(Guid id, CancellationToken cancellationToken);
}
