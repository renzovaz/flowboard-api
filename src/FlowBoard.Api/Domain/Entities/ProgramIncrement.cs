using FlowBoard.Api.Domain.Rules;

namespace FlowBoard.Api.Domain.Entities;

/// <summary>
/// Program Increment (SAFe): ciclo de planejamento com N iterações; a última é a iteração IP
/// (Innovation and Planning). Data de início e número de iterações são imutáveis (R6).
/// </summary>
public class ProgramIncrement
{
    private ProgramIncrement()
    {
    }

    public ProgramIncrement(string name, DateOnly startDate, int iterationCount, int iterationLengthWeeks, DateTime createdAt)
    {
        DomainLimits.EnsureInRange(iterationCount, DomainLimits.IterationCountMin, DomainLimits.IterationCountMax, "iterationCount");
        DomainLimits.EnsureInRange(iterationLengthWeeks, DomainLimits.IterationLengthWeeksMin, DomainLimits.IterationLengthWeeksMax, "iterationLengthWeeks");

        Name = name.Trim();
        StartDate = startDate;
        IterationCount = iterationCount;
        IterationLengthWeeks = iterationLengthWeeks;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateOnly StartDate { get; private set; }

    public int IterationCount { get; private set; }

    public int IterationLengthWeeks { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Número da iteração IP (sempre a última).</summary>
    public int IpIteration => IterationCount;

    /// <summary>Último dia do PI.</summary>
    public DateOnly EndDate => StartDate.AddDays((IterationCount * IterationLengthWeeks * 7) - 1);

    /// <summary>Calcula as datas de cada iteração a partir do início do PI.</summary>
    public IReadOnlyList<IterationInfo> GetIterations()
    {
        var days = IterationLengthWeeks * 7;
        var iterations = new List<IterationInfo>(IterationCount);
        for (var number = 1; number <= IterationCount; number++)
        {
            var start = StartDate.AddDays((number - 1) * days);
            iterations.Add(new IterationInfo(number, start, start.AddDays(days - 1), number == IpIteration));
        }

        return iterations;
    }
}

/// <summary>Dados calculados de uma iteração do PI.</summary>
public sealed record IterationInfo(int Number, DateOnly StartDate, DateOnly EndDate, bool IsIp);
