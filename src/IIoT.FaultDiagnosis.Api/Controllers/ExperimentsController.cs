using IIoT.FaultDiagnosis.Api.Contracts.Experiments;
using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Application.Diagnosis;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.FaultDiagnosis.Api.Controllers;

[ApiController]
[Route("api/experiments")]
public sealed class ExperimentsController(IExperimentService experimentService, IDiagnosisService diagnosisService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ExperimentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ExperimentDto>> Create(CreateExperimentRequest request, CancellationToken cancellationToken)
    {
        var result = await experimentService.CreateAsync(new CreateExperimentCommand(request.Name, request.ProtocolType, request.ActualFaultType, request.DeviceCount, request.TagCount, request.PollingIntervalMs, request.DurationSeconds, request.RepeatCount), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExperimentDto>> Get(Guid id, CancellationToken cancellationToken) => (await experimentService.GetAsync(id, cancellationToken)) is { } item ? Ok(item) : NotFound();

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        if (await experimentService.GetAsync(id, cancellationToken) is null) return NotFound();
        return await experimentService.StartAsync(id, cancellationToken) ? Accepted($"/api/experiments/{id}", null) : Conflict();
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, CancellationToken cancellationToken)
    {
        if (await experimentService.GetAsync(id, cancellationToken) is null) return NotFound();
        return await experimentService.StopAsync(id, cancellationToken) ? Accepted() : Conflict();
    }

    [HttpGet("{id:guid}/runs")]
    public async Task<ActionResult<IReadOnlyList<ExperimentRunDto>>> Runs(Guid id, CancellationToken cancellationToken) => (await experimentService.GetRunsAsync(id, cancellationToken)) is { } items ? Ok(items) : NotFound();

    [HttpGet("{id:guid}/records")]
    public async Task<ActionResult<IReadOnlyList<CommunicationRecordDto>>> Records(Guid id, CancellationToken cancellationToken) => (await experimentService.GetRecordsAsync(id, cancellationToken)) is { } items ? Ok(items) : NotFound();

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken) => (await experimentService.ExportAsync(id, cancellationToken)) is { } export ? File(export.Content, "text/csv; charset=utf-8", export.FileName) : NotFound();

    [HttpGet("{id:guid}/diagnosis-evaluation")]
    public async Task<IActionResult> DiagnosisEvaluation(Guid id, CancellationToken cancellationToken) => (await diagnosisService.GetEvaluationAsync(id, cancellationToken)) is { } result ? Ok(result) : NotFound();
}
