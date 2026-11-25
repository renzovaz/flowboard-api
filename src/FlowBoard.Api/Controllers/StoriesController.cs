using FlowBoard.Api.Contracts.Stories;
using FlowBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.Api.Controllers;

/// <summary>Stories e movimentação no quadro Kanban (E15–E18).</summary>
[ApiController]
[Produces("application/json")]
public sealed class StoriesController(StoryService stories) : ControllerBase
{
    /// <summary>E15 — Cria uma Story (sempre em Backlog).</summary>
    [HttpPost("api/features/{featureId:int}/stories")]
    [ProducesResponseType<StoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StoryResponse>> Create(int featureId, StoryRequest request, CancellationToken ct)
    {
        var story = await stories.CreateAsync(featureId, request, ct);
        return CreatedAtAction(nameof(Get), new { storyId = story.Id }, story);
    }

    /// <summary>E16 — Consulta uma Story.</summary>
    [HttpGet("api/stories/{storyId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<StoryResponse> Get(int storyId, CancellationToken ct) => stories.GetAsync(storyId, ct);

    /// <summary>E17 — Atualiza o planejamento (pontos, critérios, equipe, iteração).</summary>
    [HttpPut("api/stories/{storyId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<StoryResponse> Update(int storyId, StoryRequest request, CancellationToken ct) =>
        stories.UpdateAsync(storyId, request, ct);

    /// <summary>E18 — Move a Story no quadro (R1, R2, R3).</summary>
    [HttpPatch("api/stories/{storyId:int}/status")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<StoryResponse> ChangeStatus(int storyId, ChangeStoryStatusRequest request, CancellationToken ct) =>
        stories.ChangeStatusAsync(storyId, request.Status, ct);
}
