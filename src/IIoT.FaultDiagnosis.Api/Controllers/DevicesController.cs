using IIoT.FaultDiagnosis.Api.Contracts.Devices;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Protocols.Modbus;
using IIoT.FaultDiagnosis.Protocols.OpcUa;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.FaultDiagnosis.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController(IDeviceService deviceService, IDeviceConnectionService deviceConnectionService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DeviceDto>> Create(
        [FromBody] CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await deviceService.CreateAsync(
            new CreateDeviceCommand(request.Name, request.ProtocolType, request.Host, request.Port, request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> List(CancellationToken cancellationToken) =>
        Ok(await deviceService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var device = await deviceService.GetByIdAsync(id, cancellationToken);
        return device is null ? NotFound() : Ok(device);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> Update(
        Guid id,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await deviceService.UpdateAsync(
            id,
            new UpdateDeviceCommand(request.Name, request.ProtocolType, request.Host, request.Port, request.IsEnabled),
            cancellationToken);

        return device is null ? NotFound() : Ok(device);
    }

    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(DeviceConnectionTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceConnectionTestResult>> TestConnection(
        Guid id,
        [FromBody] TestModbusConnectionRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new TestModbusConnectionRequest();
        var result = await deviceConnectionService.TestModbusAsync(
            id,
            new ModbusConfiguration((byte)request.SlaveId, (ushort)request.StartAddress, (ushort)request.NumberOfPoints),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/test-opc-ua")]
    [ProducesResponseType(typeof(DeviceConnectionTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceConnectionTestResult>> TestOpcUaConnection(
        Guid id,
        [FromBody] TestOpcUaConnectionRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new TestOpcUaConnectionRequest();
        var device = await deviceService.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            return NotFound();
        }

        var endpoint = device.Host.StartsWith("opc.tcp://", StringComparison.OrdinalIgnoreCase)
            ? device.Host
            : $"opc.tcp://{device.Host}:{device.Port}";
        var result = await deviceConnectionService.TestOpcUaAsync(
            id,
            new OpcUaConfiguration(endpoint, request.NodeIds, UseSecurity: request.UseSecurity, OperationTimeoutMs: request.OperationTimeoutMs),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deviceService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
