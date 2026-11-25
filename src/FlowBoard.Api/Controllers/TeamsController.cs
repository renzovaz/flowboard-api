using FlowBoard.Api.Contracts.Teams;
using FlowBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.Api.Controllers;

/// <summary>Equipes ágeis, quadro Kanban e capacidade (E01–E06). Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/teams")]
[Produces("application/json")]
public sealed class TeamsController(TeamService teams) : ControllerBase
{
    /// <summary>E01 — Cria uma equipe.</summary>
    [HttpPost]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TeamResponse>> Create(TeamRequest request, CancellationToken ct)
    {
        var team = await teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { teamId = team.Id }, team);
    }

    /// <summary>E02 — Lista as equipes.</summary>
    [HttpGet]
    public Task<IReadOnlyList<TeamResponse>> List(CancellationToken ct) => teams.ListAsync(ct);

    /// <summary>E03 — Consulta uma equipe.</summary>
    [HttpGet("{teamId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TeamResponse> Get(int teamId, CancellationToken ct) => teams.GetAsync(teamId, ct);

    /// <summary>E04 — Atualiza nome, capacidade e limites de WIP.</summary>
    [HttpPut("{teamId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<TeamResponse> Update(int teamId, TeamRequest request, CancellationToken ct) =>
        teams.UpdateAsync(teamId, request, ct);

    /// <summary>E05 — Quadro Kanban da equipe com ocupação de WIP.</summary>
    [HttpGet("{teamId:int}/board")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<BoardResponse> Board(int teamId, CancellationToken ct) => teams.GetBoardAsync(teamId, ct);

    /// <summary>E06 — Carga planejada × capacidade por iteração de um PI.</summary>
    [HttpGet("{teamId:int}/capacity")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TeamCapacityResponse> Capacity(int teamId, [FromQuery] int programIncrementId, CancellationToken ct) =>
        teams.GetCapacityAsync(teamId, programIncrementId, ct);
}
