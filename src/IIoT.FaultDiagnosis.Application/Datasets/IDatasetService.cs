namespace IIoT.FaultDiagnosis.Application.Datasets;

public interface IDatasetService
{
    Task<DatasetExport> ExportAsync(DatasetQuery query, CancellationToken cancellationToken);
    Task<DatasetSummary> GetSummaryAsync(int minimumRunsPerFault, CancellationToken cancellationToken);
}
