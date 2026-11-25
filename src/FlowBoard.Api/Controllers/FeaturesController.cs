using FlowBoard.Api.Contracts.Features;
using FlowBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.Api.Controllers;

/// <summary>Features e priorização WSJF (E11–E14).</summary>
[ApiController]
[Produces("application/json")]
public sealed class FeaturesController(FeatureService features) : ControllerBase
{
    /// <summary>E11 — Cria uma Feature em um PI.</summary>
    [HttpPost("api/program-increments/{piId:int}/features")]
    [ProducesResponseType<FeatureResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FeatureResponse>> Create(int piId, FeatureRequest request, CancellationToken ct)
    {
        var feature = await features.CreateAsync(piId, request, ct);
        return CreatedAtAction(nameof(Get), new { featureId = feature.Id }, feature);
    }

    /// <summary>E12 — Backlog do PI ordenado por WSJF (com rank).</summary>
    [HttpGet("api/program-increments/{piId:int}/features")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<FeatureResponse>> ListByWsjf(int piId, CancellationToken ct) => features.ListByWsjfAsync(piId, ct);

    /// <summary>E13 — Consulta uma Feature.</summary>
    [HttpGet("api/features/{featureId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<FeatureResponse> Get(int featureId, CancellationToken ct) => features.GetAsync(featureId, ct);

    /// <summary>E14 — Atualiza título, descrição e campos do WSJF.</summary>
    [HttpPut("api/features/{featureId:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<FeatureResponse> Update(int featureId, FeatureRequest request, CancellationToken ct) =>
        features.UpdateAsync(featureId, request, ct);
}
