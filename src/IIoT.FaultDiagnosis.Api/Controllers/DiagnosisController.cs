using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Application.Rag;
using Microsoft.AspNetCore.Mvc;
namespace IIoT.FaultDiagnosis.Api.Controllers;
[ApiController, Route("api/communication-records")]
public sealed class DiagnosisController(IDiagnosisService service, IRagDiagnosisService ragService):ControllerBase
{
    [HttpPost("{id:guid}/diagnose")]
    public async Task<IActionResult> Diagnose(Guid id,CancellationToken ct) => (await service.DiagnoseAsync(id,ct)) is { } result ? Ok(result) : NotFound();

    [HttpPost("{id:guid}/rag-diagnose")]
    public async Task<IActionResult> RagDiagnose(Guid id, CancellationToken ct) => (await ragService.DiagnoseAsync(id, ct)) is { } result ? Ok(result) : NotFound();
}
