using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Exceptions;

namespace FlowBoard.Api.Domain.Rules;

/// <summary>
/// R1 — Fluxo Kanban: Backlog → Ready → InProgress → Review → Done,
/// com retrabalho permitido apenas de Review para InProgress. Done é final.
/// </summary>
public static class KanbanWorkflow
{
    private static readonly HashSet<(StoryStatus From, StoryStatus To)> AllowedTransitions =
    [
        (StoryStatus.Backlog, StoryStatus.Ready),
        (StoryStatus.Ready, StoryStatus.InProgress),
        (StoryStatus.InProgress, StoryStatus.Review),
        (StoryStatus.Review, StoryStatus.Done),
        (StoryStatus.Review, StoryStatus.InProgress), // retrabalho
    ];

    public static bool CanMove(StoryStatus from, StoryStatus to) => AllowedTransitions.Contains((from, to));

    /// <summary>Lança HTTP 409 INVALID_TRANSITION se a transição não for permitida (inclui mesmo status).</summary>
    public static void EnsureCanMove(StoryStatus from, StoryStatus to)
    {
        if (!CanMove(from, to))
        {
            throw new ConflictException(
                ErrorCodes.InvalidTransition,
                "Transição não permitida",
                $"Transição de {from} para {to} não permitida.");
        }
    }
}
