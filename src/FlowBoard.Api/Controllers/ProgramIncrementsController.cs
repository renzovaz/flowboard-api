using FlowBoard.Api.Contracts.ProgramIncrements;
using FlowBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.Api.Controllers;

/// <summary>Program Increments e progresso (E07–E10).</summary>
[ApiController]
[Route("api/program-increments")]
[Produces("application/json")]
public sealed class ProgramIncrementsController(ProgramIncrementService programIncrements) : ControllerBase
{
    /// <summary>E07 — Cria um PI (gera as iterações; a última é a IP).</summary>
    [HttpPost]
    [ProducesResponseType<ProgramIncrementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProgramIncrementResponse>> Create(CreateProgramIncrementRequest request, CancellationToken ct)
    {
        var pi = await programIncrements.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { piId = pi.Id }, pi);
    }

    /// <summary>E08 — Lista os PIs.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ProgramIncrementResponse>> List(CancellationToken ct) => programIncrements.ListAsync(ct);

    /// <summary>E09 — Consulta um PI com as datas de cada iteração.</summary>
    [HttpGet("{piId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProgramIncrementResponse> Get(int piId, CancellationToken ct) => programIncrements.GetAsync(piId, ct);

    /// <summary>E10 — Progresso por Feature, equipe e PI (R8).</summary>
    [HttpGet("{piId:int}/progress")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ProgressResponse> Progress(int piId, CancellationToken ct) => programIncrements.GetProgressAsync(piId, ct);
}
