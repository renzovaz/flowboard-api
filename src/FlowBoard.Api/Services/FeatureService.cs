using FlowBoard.Api.Contracts.Features;
using FlowBoard.Api.Data;
using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Services;

/// <summary>Casos de uso de Features e priorização por WSJF (R7).</summary>
public sealed class FeatureService(FlowBoardDbContext db, TimeProvider clock)
{
    public async Task<FeatureResponse> CreateAsync(int piId, FeatureRequest request, CancellationToken ct)
    {
        if (!await db.ProgramIncrements.AnyAsync(p => p.Id == piId, ct))
        {
            throw new NotFoundException("Program Increment", piId);
        }

        var feature = new Feature(piId, request.Title, request.Description, request.BusinessValue,
            request.TimeCriticality, request.RiskReduction, request.JobSize, clock.GetUtcNow().UtcDateTime);
        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return FeatureResponse.From(feature);
    }

    /// <summary>Backlog do PI ordenado por WSJF, com desempate determinístico (ADR-001).</summary>
    public async Task<IReadOnlyList<FeatureResponse>> ListByWsjfAsync(int piId, CancellationToken ct)
    {
        if (!await db.ProgramIncrements.AnyAsync(p => p.Id == piId, ct))
        {
            throw new NotFoundException("Program Increment", piId);
        }

        var features = await db.Features.AsNoTracking().Where(f => f.ProgramIncrementId == piId).ToListAsync(ct);
        return WsjfCalculator.Order(features).Select((f, index) => FeatureResponse.From(f, index + 1)).ToList();
    }

    public async Task<FeatureResponse> GetAsync(int featureId, CancellationToken ct) =>
        FeatureResponse.From(await FindAsync(featureId, ct));

    public async Task<FeatureResponse> UpdateAsync(int featureId, FeatureRequest request, CancellationToken ct)
    {
        var feature = await FindAsync(featureId, ct);
        feature.Update(request.Title, request.Description, request.BusinessValue,
            request.TimeCriticality, request.RiskReduction, request.JobSize);
        await db.SaveChangesAsync(ct);
        return FeatureResponse.From(feature);
    }

    private async Task<Feature> FindAsync(int featureId, CancellationToken ct) =>
        await db.Features.SingleOrDefaultAsync(f => f.Id == featureId, ct) ?? throw new NotFoundException("Feature", featureId);
}
