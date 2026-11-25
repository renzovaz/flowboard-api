namespace FlowBoard.Api.Domain.Entities;

/// <summary>Colunas do quadro Kanban (fixas para todas as equipes).</summary>
public enum StoryStatus
{
    Backlog,
    Ready,
    InProgress,
    Review,
    Done,
}
