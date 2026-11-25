namespace FlowBoard.Api.Domain.Exceptions;

/// <summary>
/// Códigos de erro estáveis retornados no campo "errorCode" do Problem Details.
/// Os testes verificam estes códigos (e não as mensagens em português).
/// Catálogo completo: docs/errors.md.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string DuplicateName = "DUPLICATE_NAME";
    public const string InvalidTransition = "INVALID_TRANSITION";              // R1
    public const string WipLimitExceeded = "WIP_LIMIT_EXCEEDED";              // R2
    public const string DefinitionOfReadyNotMet = "DEFINITION_OF_READY_NOT_MET"; // R3
    public const string DefinitionOfReadyViolated = "DEFINITION_OF_READY_VIOLATED"; // R4 / RC1
    public const string InvalidStoryPoints = "INVALID_STORY_POINTS";          // R4
    public const string StoryTooLarge = "STORY_TOO_LARGE";                    // R4
    public const string CapacityExceeded = "CAPACITY_EXCEEDED";               // R5
    public const string InvalidIteration = "INVALID_ITERATION";               // R6
    public const string IpIterationNotPlannable = "IP_ITERATION_NOT_PLANNABLE"; // R6
    public const string InvalidWsjfValue = "INVALID_WSJF_VALUE";              // R7
    public const string InvalidRange = "INVALID_RANGE";
    public const string TeamChangeNotAllowed = "TEAM_CHANGE_NOT_ALLOWED";     // RC2
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";
    public const string InternalError = "INTERNAL_ERROR";
}
