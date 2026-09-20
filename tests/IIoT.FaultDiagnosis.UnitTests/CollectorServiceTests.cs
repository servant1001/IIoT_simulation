using IIoT.FaultDiagnosis.Application.Collection;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Protocols.Abstractions;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class CollectorServiceTests
{
    [Fact]
    public async Task CollectEnabledDevicesAsync_RetriesAndPersistsFinalRecord()
    {
        var enabledDevice = CreateDevice(isEnabled: true);
        var disabledDevice = CreateDevice(isEnabled: false);
        var deviceRepository = new InMemoryDeviceRepository(enabledDevice, disabledDevice);
        var recordRepository = new InMemoryCommunicationRecordRepository();
        var adapter = new RetryThenSuccessModbusAdapter();
        var service = new CollectorService(
            deviceRepository,
            recordRepository,
            [adapter],
            Options.Create(new CollectorOptions { MaxRetry = 1, RetryDelayMs = 0 }),
            TimeProvider.System,
            NullLogger<CollectorService>.Instance);

        var results = await service.CollectEnabledDevicesAsync(CollectorExecutionContext.None, CancellationToken.None);

        var result = Assert.Single(results);
        var record = Assert.Single(recordRepository.Records);
        Assert.Equal(enabledDevice.Id, result.DeviceId);
        Assert.True(result.Success);
        Assert.Equal(1, result.RetryCount);
        Assert.Equal(1, record.RetryCount);
        Assert.Equal(CommunicationStatus.Success, record.Status);
        Assert.Equal(2, adapter.ConnectCalls);
        Assert.Equal(2, adapter.DisconnectCalls);
    }

    [Fact]
    public async Task CaptureAsync_PersistsStandaloneMetric()
    {
        var repository = new InMemoryMetricRepository();
        var service = new MetricService(
            repository,
            new ProcessMetricsSampler(TimeProvider.System),
            NullLogger<MetricService>.Instance);

        var metric = await service.CaptureAsync(CollectorExecutionContext.None, CancellationToken.None);

        Assert.Same(metric, Assert.Single(repository.Metrics));
        Assert.Null(metric.ExperimentRunId);
        Assert.True(metric.MemoryMb > 0);
        Assert.True(metric.CpuPercent >= 0);
    }

    [Fact]
    public async Task CollectEnabledDevicesAsync_ContinuesAfterUnexpectedDeviceFailure()
    {
        var firstDevice = CreateDevice(isEnabled: true);
        var secondDevice = CreateDevice(isEnabled: true);
        var records = new InMemoryCommunicationRecordRepository();
        var service = new CollectorService(
            new InMemoryDeviceRepository(firstDevice, secondDevice),
            records,
            [new ThrowThenSuccessModbusAdapter()],
            Options.Create(new CollectorOptions { MaxRetry = 0 }),
            TimeProvider.System,
            NullLogger<CollectorService>.Instance);

        var results = await service.CollectEnabledDevicesAsync(CollectorExecutionContext.None, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(secondDevice.Id, result.DeviceId);
        Assert.Single(records.Records);
    }

    [Fact]
    public async Task CollectEnabledDevicesAsync_UsesOpcUaAdapterForOpcUaDevice()
    {
        var device = CreateDevice(isEnabled: true, ProtocolType.OpcUa);
        var records = new InMemoryCommunicationRecordRepository();
        var adapter = new SuccessOpcUaAdapter();
        var service = new CollectorService(
            new InMemoryDeviceRepository(device),
            records,
            [adapter],
            Options.Create(new CollectorOptions { MaxRetry = 0 }),
            TimeProvider.System,
            NullLogger<CollectorService>.Instance);

        var result = Assert.Single(await service.CollectEnabledDevicesAsync(CollectorExecutionContext.None, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(1, adapter.ConnectCalls);
        Assert.Equal(1, adapter.CollectCalls);
        Assert.Equal(1, adapter.DisconnectCalls);
        Assert.Equal(ProtocolType.OpcUa, Assert.Single(records.Records).ProtocolType);
    }

    [Fact]
    public async Task CollectEnabledDevicesAsync_UsesMqttAdapterForMqttDevice()
    {
        var device = CreateDevice(isEnabled: true, ProtocolType.Mqtt);
        var records = new InMemoryCommunicationRecordRepository();
        var adapter = new SuccessMqttAdapter();
        var service = new CollectorService(
            new InMemoryDeviceRepository(device),
            records,
            [adapter],
            Options.Create(new CollectorOptions { MaxRetry = 0 }),
            TimeProvider.System,
            NullLogger<CollectorService>.Instance);

        var result = Assert.Single(await service.CollectEnabledDevicesAsync(CollectorExecutionContext.None, CancellationToken.None));

        Assert.True(result.Success);
        Assert.Equal(1, adapter.ConnectCalls);
        Assert.Equal(1, adapter.CollectCalls);
        Assert.Equal(1, adapter.DisconnectCalls);
        Assert.Equal(ProtocolType.Mqtt, Assert.Single(records.Records).ProtocolType);
    }

    [Fact]
    public void CollectorExecutionContext_RequiresExperimentAndRunTogether()
    {
        var context = new CollectorExecutionContext(Guid.NewGuid(), null);

        Assert.Throws<InvalidOperationException>(context.Validate);
    }

    private static Device CreateDevice(bool isEnabled, ProtocolType protocolType = ProtocolType.ModbusTcp) => new(
        Guid.NewGuid(),
        "Collector test device",
        protocolType,
        "127.0.0.1",
        502,
        isEnabled,
        DateTimeOffset.UtcNow);

    private sealed class InMemoryDeviceRepository(params Device[] devices) : IDeviceRepository
    {
        public Task AddAsync(Device device, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(devices.SingleOrDefault(device => device.Id == id));
        public Task<IReadOnlyList<Device>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Device>>(devices);
        public Task RemoveAsync(Device device, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryCommunicationRecordRepository : ICommunicationRecordRepository
    {
        public List<CommunicationRecord> Records { get; } = [];
        public Task AddAsync(CommunicationRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryMetricRepository : IMetricRepository
    {
        public List<SystemMetric> Metrics { get; } = [];
        public Task AddAsync(SystemMetric metric, CancellationToken cancellationToken)
        {
            Metrics.Add(metric);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RetryThenSuccessModbusAdapter : IModbusProtocolAdapter
    {
        public int ConnectCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public ProtocolType ProtocolType => ProtocolType.ModbusTcp;

        public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken)
        {
            ConnectCalls++;
            return Task.FromResult(ConnectCalls == 1
                ? ConnectionResult.Failed(FaultType.ConnectionRefused, "ConnectionRefused", "Connection refused")
                : ConnectionResult.Connected());
        }

        public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken) =>
            CollectAsync(device, ModbusConfiguration.Default, cancellationToken);

        public Task<CollectionResult> CollectAsync(Device device, ModbusConfiguration configuration, CancellationToken cancellationToken) =>
            Task.FromResult(new CollectionResult(true, FaultType.None, null, null, "request", "[1]", "[1]"));

        public Task DisconnectAsync(Device device, CancellationToken cancellationToken)
        {
            DisconnectCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowThenSuccessModbusAdapter : IModbusProtocolAdapter
    {
        private int _connectCalls;

        public ProtocolType ProtocolType => ProtocolType.ModbusTcp;

        public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken)
        {
            _connectCalls++;
            if (_connectCalls == 1)
            {
                throw new InvalidOperationException("Unexpected adapter failure");
            }

            return Task.FromResult(ConnectionResult.Connected());
        }

        public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken) =>
            CollectAsync(device, ModbusConfiguration.Default, cancellationToken);

        public Task<CollectionResult> CollectAsync(Device device, ModbusConfiguration configuration, CancellationToken cancellationToken) =>
            Task.FromResult(new CollectionResult(true, FaultType.None, null, null, "request", "[1]", "[1]"));

        public Task DisconnectAsync(Device device, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SuccessOpcUaAdapter : IProtocolAdapter
    {
        public int ConnectCalls { get; private set; }
        public int CollectCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public ProtocolType ProtocolType => ProtocolType.OpcUa;

        public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken)
        {
            ConnectCalls++;
            return Task.FromResult(ConnectionResult.Connected());
        }

        public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken)
        {
            CollectCalls++;
            return Task.FromResult(new CollectionResult(true, FaultType.None, null, null, "NodeId=i=2258", "42", "42"));
        }

        public Task DisconnectAsync(Device device, CancellationToken cancellationToken)
        {
            DisconnectCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessMqttAdapter : IProtocolAdapter
    {
        public int ConnectCalls { get; private set; }
        public int CollectCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public ProtocolType ProtocolType => ProtocolType.Mqtt;

        public Task<ConnectionResult> ConnectAsync(Device device, CancellationToken cancellationToken)
        {
            ConnectCalls++;
            return Task.FromResult(ConnectionResult.Connected());
        }

        public Task<CollectionResult> CollectAsync(Device device, CancellationToken cancellationToken)
        {
            CollectCalls++;
            return Task.FromResult(new CollectionResult(true, FaultType.None, null, null, "Topic=factory/machine01/data", "{}", "{}"));
        }

        public Task DisconnectAsync(Device device, CancellationToken cancellationToken)
        {
            DisconnectCalls++;
            return Task.CompletedTask;
        }
    }
}
