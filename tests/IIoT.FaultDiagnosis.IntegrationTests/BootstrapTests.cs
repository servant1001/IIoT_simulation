using System.Net;
using System.Net.Http.Json;
using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Domain.Enums;
using IIoT.FaultDiagnosis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IIoT.FaultDiagnosis.IntegrationTests;

public sealed class BootstrapTests : IClassFixture<PostgresWebApplicationFactory>
{
    private readonly PostgresWebApplicationFactory _factory;

    public BootstrapTests(PostgresWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health", TestContextCancellation);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContextCancellation));
    }

    [Fact]
    public async Task DeviceEndpoints_ProvideCrudPersistence()
    {
        using var client = _factory.CreateClient();
        var createRequest = new
        {
            Name = "Integration Test Device",
            ProtocolType = ProtocolType.ModbusTcp,
            Host = "127.0.0.1",
            Port = 502,
            IsEnabled = true
        };

        using var createResponse = await client.PostAsJsonAsync("/api/devices", createRequest, TestContextCancellation);
        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created,
            $"Expected 201 but received {(int)createResponse.StatusCode}: {await createResponse.Content.ReadAsStringAsync(TestContextCancellation)}");
        var created = await createResponse.Content.ReadFromJsonAsync<DeviceDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(created);

        using var getResponse = await client.GetAsync($"/api/devices/{created!.Id}", TestContextCancellation);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var stored = await getResponse.Content.ReadFromJsonAsync<DeviceDto>(cancellationToken: TestContextCancellation);
        Assert.Equal("Integration Test Device", stored!.Name);
        Assert.Equal(ProtocolType.ModbusTcp, stored.ProtocolType);

        var updateRequest = new
        {
            Name = "Updated Integration Device",
            ProtocolType = ProtocolType.OpcUa,
            Host = "localhost",
            Port = 4840,
            IsEnabled = false
        };
        using var updateResponse = await client.PutAsJsonAsync($"/api/devices/{created.Id}", updateRequest, TestContextCancellation);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DeviceDto>(cancellationToken: TestContextCancellation);
        Assert.Equal("Updated Integration Device", updated!.Name);
        Assert.Equal(ProtocolType.OpcUa, updated.ProtocolType);
        Assert.False(updated.IsEnabled);

        using var deleteResponse = await client.DeleteAsync($"/api/devices/{created.Id}", TestContextCancellation);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await client.GetAsync($"/api/devices/{created.Id}", TestContextCancellation);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task TestConnection_PersistsNormalizedFailureRecord()
    {
        var closedPort = GetClosedLoopbackPort();
        using var client = _factory.CreateClient();
        var createRequest = new
        {
            Name = "Modbus Failure Test Device",
            ProtocolType = ProtocolType.ModbusTcp,
            Host = "127.0.0.1",
            Port = closedPort,
            IsEnabled = true
        };

        using var createResponse = await client.PostAsJsonAsync("/api/devices", createRequest, TestContextCancellation);
        var device = await createResponse.Content.ReadFromJsonAsync<DeviceDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(device);

        using var testResponse = await client.PostAsJsonAsync(
            $"/api/devices/{device!.Id}/test",
            new { SlaveId = 1, StartAddress = 0, NumberOfPoints = 2 },
            TestContextCancellation);
        Assert.Equal(HttpStatusCode.OK, testResponse.StatusCode);
        var result = await testResponse.Content.ReadFromJsonAsync<ConnectionTestResponse>(cancellationToken: TestContextCancellation);
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.NotEqual(FaultType.None, result.FaultType);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FaultDiagnosisDbContext>();
            var record = await dbContext.CommunicationRecords.SingleAsync(record => record.Id == result.CommunicationRecordId, TestContextCancellation);
            Assert.Equal(device.Id, record.DeviceId);
            Assert.Equal(CommunicationStatus.Failed, record.Status);
            Assert.NotEqual(FaultType.None, record.FaultType);
            Assert.Null(record.ExperimentId);
            Assert.Null(record.ExperimentRunId);

            dbContext.CommunicationRecords.Remove(record);
            await dbContext.SaveChangesAsync(TestContextCancellation);
        }

        using var deleteResponse = await client.DeleteAsync($"/api/devices/{device.Id}", TestContextCancellation);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ExperimentEndpoints_RunNormalCollectionAndExportCsv()
    {
        using var client = _factory.CreateClient();
        var request = new { Name = "Phase4 NORMAL integration", ProtocolType = ProtocolType.ModbusTcp, ActualFaultType = FaultType.None, DeviceCount = 1, TagCount = 6, PollingIntervalMs = 250, DurationSeconds = 2, RepeatCount = 1 };
        using var createdResponse = await client.PostAsJsonAsync("/api/experiments", request, TestContextCancellation);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var experiment = await createdResponse.Content.ReadFromJsonAsync<ExperimentDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(experiment);
        using var startResponse = await client.PostAsync($"/api/experiments/{experiment!.Id}/start", null, TestContextCancellation);
        Assert.Equal(HttpStatusCode.Accepted, startResponse.StatusCode);
        ExperimentDto? completed = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(250, TestContextCancellation);
            completed = await client.GetFromJsonAsync<ExperimentDto>($"/api/experiments/{experiment.Id}", TestContextCancellation);
            if (completed?.Status == ExperimentStatus.Completed) break;
        }
        Assert.Equal(ExperimentStatus.Completed, completed?.Status);
        var runs = await client.GetFromJsonAsync<List<ExperimentRunDto>>($"/api/experiments/{experiment.Id}/runs", TestContextCancellation);
        var run = Assert.Single(runs!);
        Assert.True(run.SuccessCount > 0);
        using var export = await client.GetAsync($"/api/experiments/{experiment.Id}/export", TestContextCancellation);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Contains("experiment_id,run_id", await export.Content.ReadAsStringAsync(TestContextCancellation));
    }

    [Fact]
    public async Task ExperimentEndpoints_RunConnectionRefusedCollection()
    {
        using var client = _factory.CreateClient();
        var request = new { Name = "Phase4 CONNECTION_REFUSED integration", ProtocolType = ProtocolType.ModbusTcp, ActualFaultType = FaultType.ConnectionRefused, DeviceCount = 1, TagCount = 6, PollingIntervalMs = 250, DurationSeconds = 1, RepeatCount = 1 };
        using var createdResponse = await client.PostAsJsonAsync("/api/experiments", request, TestContextCancellation);
        var experiment = await createdResponse.Content.ReadFromJsonAsync<ExperimentDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(experiment);
        using var startResponse = await client.PostAsync($"/api/experiments/{experiment!.Id}/start", null, TestContextCancellation);
        Assert.Equal(HttpStatusCode.Accepted, startResponse.StatusCode);
        ExperimentDto? completed = null;
        for (var attempt = 0; attempt < 120; attempt++) { await Task.Delay(250, TestContextCancellation); completed = await client.GetFromJsonAsync<ExperimentDto>($"/api/experiments/{experiment.Id}", TestContextCancellation); if (completed?.Status == ExperimentStatus.Completed) break; }
        Assert.Equal(ExperimentStatus.Completed, completed?.Status);
        var records = await client.GetFromJsonAsync<List<CommunicationRecordDto>>($"/api/experiments/{experiment.Id}/records", TestContextCancellation);
        Assert.Contains(records!, record => record.FaultType == FaultType.ConnectionRefused && record.Status == CommunicationStatus.Failed);
    }

    [Fact]
    public async Task ExperimentEndpoints_RunIllegalAddressCollection()
    {
        using var client = _factory.CreateClient();
        var request = new { Name = "Phase4 ILLEGAL_ADDRESS integration", ProtocolType = ProtocolType.ModbusTcp, ActualFaultType = FaultType.IllegalAddress, DeviceCount = 1, TagCount = 6, PollingIntervalMs = 250, DurationSeconds = 1, RepeatCount = 1 };
        using var create = await client.PostAsJsonAsync("/api/experiments", request, TestContextCancellation);
        var experiment = await create.Content.ReadFromJsonAsync<ExperimentDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(experiment);
        using var start = await client.PostAsync($"/api/experiments/{experiment!.Id}/start", null, TestContextCancellation);
        Assert.True(start.StatusCode == HttpStatusCode.Accepted, await start.Content.ReadAsStringAsync(TestContextCancellation));
        await Task.Delay(10000, TestContextCancellation);
        var records = await client.GetFromJsonAsync<List<CommunicationRecordDto>>($"/api/experiments/{experiment.Id}/records", TestContextCancellation);
        Assert.Contains(records!, record => record.FaultType == FaultType.IllegalAddress && record.Status == CommunicationStatus.Failed);
    }

    [Fact]
    public async Task ExperimentEndpoints_RunTimeoutCollection()
    {
        using var client = _factory.CreateClient();
        var request = new { Name = "Phase4 TIMEOUT integration", ProtocolType = ProtocolType.ModbusTcp, ActualFaultType = FaultType.Timeout, DeviceCount = 1, TagCount = 6, PollingIntervalMs = 250, DurationSeconds = 1, RepeatCount = 1 };
        using var create = await client.PostAsJsonAsync("/api/experiments", request, TestContextCancellation);
        var experiment = await create.Content.ReadFromJsonAsync<ExperimentDto>(cancellationToken: TestContextCancellation);
        Assert.NotNull(experiment);
        using var start = await client.PostAsync($"/api/experiments/{experiment!.Id}/start", null, TestContextCancellation);
        Assert.True(start.StatusCode == HttpStatusCode.Accepted, await start.Content.ReadAsStringAsync(TestContextCancellation));
        await Task.Delay(15000, TestContextCancellation);
        var records = await client.GetFromJsonAsync<List<CommunicationRecordDto>>($"/api/experiments/{experiment.Id}/records", TestContextCancellation);
        Assert.Contains(records!, record => record.FaultType == FaultType.Timeout && record.Status == CommunicationStatus.Timeout);
    }

    private static int GetClosedLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static CancellationToken TestContextCancellation => CancellationToken.None;

    private sealed record ConnectionTestResponse(Guid CommunicationRecordId, bool Success, FaultType FaultType);
}
