using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Api.Data.Configurations;

/// <summary>Mapeamento da tabela "program_increments".</summary>
public class ProgramIncrementConfiguration : IEntityTypeConfiguration<ProgramIncrement>
{
    public void Configure(EntityTypeBuilder<ProgramIncrement> builder)
    {
        builder.ToTable("program_increments", t =>
        {
            t.HasCheckConstraint("ck_program_increments_iteration_count",
                $"iteration_count BETWEEN {DomainLimits.IterationCountMin} AND {DomainLimits.IterationCountMax}");
            t.HasCheckConstraint("ck_program_increments_iteration_length_weeks",
                $"iteration_length_weeks BETWEEN {DomainLimits.IterationLengthWeeksMin} AND {DomainLimits.IterationLengthWeeksMax}");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(DomainLimits.ProgramIncrementNameMaxLength).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        // Propriedades calculadas: não viram colunas.
        builder.Ignore(p => p.IpIteration);
        builder.Ignore(p => p.EndDate);
    }
}
