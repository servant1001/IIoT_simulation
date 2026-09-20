namespace IIoT.FaultDiagnosis.Application.Collection;

public interface ICollectorService
{
    Task<IReadOnlyList<CollectorDeviceResult>> CollectEnabledDevicesAsync(
        CollectorExecutionContext context,
        CancellationToken cancellationToken);
}
