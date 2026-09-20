namespace IIoT.FaultDiagnosis.Application.Datasets;

public interface IDatasetRepository
{
    Task<IReadOnlyList<DatasetRecord>> GetRecordsAsync(CancellationToken cancellationToken);
}
