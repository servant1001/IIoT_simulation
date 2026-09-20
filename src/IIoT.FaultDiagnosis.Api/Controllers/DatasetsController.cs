using IIoT.FaultDiagnosis.Application.Datasets;
using IIoT.FaultDiagnosis.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace IIoT.FaultDiagnosis.Api.Controllers;

[ApiController]
[Route("api/datasets/communication-records")]
public sealed class DatasetsController(IDatasetService datasetService) : ControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] ProtocolType? protocolType,
        [FromQuery] FaultType? actualFaultType,
        [FromQuery] bool onlyDiagnosed,
        CancellationToken cancellationToken)
    {
        var export = await datasetService.ExportAsync(new DatasetQuery(protocolType, actualFaultType, onlyDiagnosed), cancellationToken);
        return File(export.Content, "text/csv; charset=utf-8", export.FileName);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DatasetSummary>> Summary(
        [FromQuery] int minimumRunsPerFault = 100,
        CancellationToken cancellationToken = default)
    {
        if (minimumRunsPerFault < 1)
        {
            return BadRequest(new { error = "minimumRunsPerFault must be at least 1." });
        }

        return Ok(await datasetService.GetSummaryAsync(minimumRunsPerFault, cancellationToken));
    }
}
