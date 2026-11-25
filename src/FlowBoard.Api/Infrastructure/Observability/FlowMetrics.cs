using FlowBoard.Api.Data;
using FlowBoard.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace FlowBoard.Api.Infrastructure.Observability;

/// <summary>
/// Métricas de fluxo Kanban expostas em /metrics (formato Prometheus), inspiradas nas
/// Flow Metrics do SAFe: Flow Load (WIP), Flow Velocity (throughput) e Flow Time (cycle time).
/// Rótulos limitados a equipe e coluna (nunca o Id da Story) para controlar a cardinalidade.
/// </summary>
public sealed class FlowMetrics
{
    private static readonly Counter StoryMoves = Metrics.CreateCounter(
        "flowboard_story_moves_total", "Movimentações de Stories entre colunas do quadro.",
        new CounterConfiguration { LabelNames = ["from", "to"] });

    private static readonly Counter WipRejections = Metrics.CreateCounter(
        "flowboard_wip_rejections_total", "Movimentações recusadas por limite de WIP (R2).",
        new CounterConfiguration { LabelNames = ["team"] });

    private static readonly Counter StoriesCompleted = Metrics.CreateCounter(
        "flowboard_stories_completed_total", "Stories concluídas (throughput / Flow Velocity).",
        new CounterConfiguration { LabelNames = ["team"] });

    private static readonly Histogram CycleTime = Metrics.CreateHistogram(
        "flowboard_story_cycle_time_seconds", "Tempo entre a primeira entrada em InProgress e Done (Flow Time).",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(start: 1, factor: 4, count: 12) });

    private static readonly Gauge WipCurrent = Metrics.CreateGauge(
        "flowboard_wip_current", "Stories por equipe e coluna no momento da coleta (Flow Load).",
        new GaugeConfiguration { LabelNames = ["team", "column"] });

    public void StoryMoved(Story story, StoryStatus from)
    {
        StoryMoves.WithLabels(from.ToString(), story.Status.ToString()).Inc();
        if (story.Status == StoryStatus.Done)
        {
            var team = story.TeamId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none";
            StoriesCompleted.WithLabels(team).Inc();
            if (story is { StartedAt: not null, CompletedAt: not null })
            {
                CycleTime.Observe((story.CompletedAt.Value - story.StartedAt.Value).TotalSeconds);
            }
        }
    }

    public void WipRejected(int teamId) =>
        WipRejections.WithLabels(teamId.ToString(System.Globalization.CultureInfo.InvariantCulture)).Inc();

    /// <summary>
    /// Calcula o WIP atual NO MOMENTO DA COLETA (uma consulta agrupada).
    /// Se o banco estiver indisponível, a métrica é omitida e o /metrics continua respondendo.
    /// </summary>
    public static void RegisterWipCollector(IServiceProvider services)
    {
        Metrics.DefaultRegistry.AddBeforeCollectCallback(async cancel =>
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
                var rows = await db.Stories.AsNoTracking()
                    .Where(s => s.TeamId != null)
                    .GroupBy(s => new { s.TeamId, s.Status })
                    .Select(g => new { g.Key.TeamId, g.Key.Status, Count = g.Count() })
                    .ToListAsync(cancel);

                // Zera todas as colunas das equipes presentes: colunas que esvaziaram não ficam com valor antigo.
                foreach (var teamId in rows.Select(r => r.TeamId!.Value).Distinct())
                {
                    foreach (var column in Enum.GetValues<StoryStatus>())
                    {
                        WipCurrent.WithLabels(teamId.ToString(System.Globalization.CultureInfo.InvariantCulture), column.ToString()).Set(0);
                    }
                }

                foreach (var row in rows)
                {
                    WipCurrent.WithLabels(row.TeamId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Status.ToString())
                        .Set(row.Count);
                }
            }
#pragma warning disable CA1031 // Métrica opcional: falha na coleta não pode derrubar o /metrics.
            catch (Exception)
#pragma warning restore CA1031
            {
                // Banco indisponível: a métrica é omitida nesta coleta (o /health/ready reporta o problema).
            }
        });
    }
}
