using FlowBoard.Api.Domain.Exceptions;
using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Domain.Entities;

/// <summary>
/// User Story: item de trabalho de uma Feature, atribuído a uma equipe e a uma iteração,
/// que percorre as colunas do quadro Kanban.
/// </summary>
public class Story
{
    private Story()
    {
    }

    /// <summary>Cria a Story sempre na coluna Backlog.</summary>
    public Story(int featureId, ProgramIncrement programIncrement, string title, string? acceptanceCriteria,
        int? storyPoints, int? teamId, int? iteration, DateTime createdAt)
    {
        FeatureId = featureId;
        Status = StoryStatus.Backlog;
        CreatedAt = createdAt;
        ApplyPlanning(programIncrement, title, acceptanceCriteria, storyPoints, teamId, iteration);
    }

    public int Id { get; private set; }

    /// <summary>Feature dona da Story. Imutável: impede que a Story troque de PI.</summary>
    public int FeatureId { get; private set; }

    public int? TeamId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? AcceptanceCriteria { get; private set; }

    public int? StoryPoints { get; private set; }

    public int? Iteration { get; private set; }

    public StoryStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Primeira entrada em InProgress (ADR-007): não muda em caso de retrabalho.</summary>
    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Atualiza os dados de planejamento, aplicando R4, R6, RC1 e RC2.
    /// </summary>
    public void UpdatePlanning(ProgramIncrement programIncrement, string title, string? acceptanceCriteria,
        int? storyPoints, int? teamId, int? iteration)
    {
        // RC2: a equipe só pode mudar enquanto a Story está em Backlog ou Ready.
        if (teamId != TeamId && Status is not (StoryStatus.Backlog or StoryStatus.Ready))
        {
            throw new ConflictException(
                ErrorCodes.TeamChangeNotAllowed,
                "Troca de equipe não permitida",
                $"A equipe só pode ser alterada enquanto a Story está em Backlog ou Ready (status atual: {Status}).");
        }

        // R4 + RC1: depois de sair do Backlog, os campos exigidos pela Definition of Ready não podem ser apagados.
        if (Status != StoryStatus.Backlog)
        {
            var missing = DefinitionOfReady.GetMissingCriteria(storyPoints, acceptanceCriteria, teamId, iteration);
            if (missing.Count > 0)
            {
                throw new BusinessRuleException(
                    ErrorCodes.DefinitionOfReadyViolated,
                    "Definition of Ready violada",
                    "Depois de sair do Backlog, a Story precisa manter pontos, critérios de aceite, equipe e iteração.",
                    new Dictionary<string, object?> { ["missingCriteria"] = missing });
            }
        }

        ApplyPlanning(programIncrement, title, acceptanceCriteria, storyPoints, teamId, iteration);
    }

    /// <summary>
    /// Move a Story para outra coluna aplicando R1 (transição) e R3 (DoR).
    /// O limite de WIP (R2) depende das outras Stories da equipe e é verificado pelo StoryService
    /// antes desta chamada, com o lock da equipe.
    /// </summary>
    public void MoveTo(StoryStatus target, DateTime now)
    {
        KanbanWorkflow.EnsureCanMove(Status, target);

        if (target == StoryStatus.Ready)
        {
            DefinitionOfReady.Ensure(this);
        }

        Status = target;

        if (target == StoryStatus.InProgress && StartedAt is null)
        {
            StartedAt = now;
        }

        if (target == StoryStatus.Done)
        {
            CompletedAt = now;
        }
    }

    private void ApplyPlanning(ProgramIncrement programIncrement, string title, string? acceptanceCriteria,
        int? storyPoints, int? teamId, int? iteration)
    {
        StoryPointsPolicy.Validate(storyPoints);                 // R4
        IterationPolicy.Validate(iteration, programIncrement);   // R6

        Title = title.Trim();
        AcceptanceCriteria = string.IsNullOrWhiteSpace(acceptanceCriteria) ? null : acceptanceCriteria.Trim();
        StoryPoints = storyPoints;
        TeamId = teamId;
        Iteration = iteration;
    }
}
