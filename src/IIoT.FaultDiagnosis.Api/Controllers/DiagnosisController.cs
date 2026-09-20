using IIoT.FaultDiagnosis.Application.Diagnosis;
using Microsoft.AspNetCore.Mvc;
namespace IIoT.FaultDiagnosis.Api.Controllers;
[ApiController, Route("api/communication-records")]
public sealed class DiagnosisController(IDiagnosisService service):ControllerBase
{ [HttpPost("{id:guid}/diagnose")] public async Task<IActionResult> Diagnose(Guid id,CancellationToken ct) => (await service.DiagnoseAsync(id,ct)) is { } result ? Ok(result) : NotFound(); }
