using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Api.Data.Configurations;

/// <summary>Mapeamento da tabela "teams" (colunas, índice único e CHECKs).</summary>
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams", t =>
        {
            // Defesa em profundidade: o banco também recusa valores fora das faixas.
            t.HasCheckConstraint("ck_teams_capacity_points",
                $"capacity_points BETWEEN {DomainLimits.CapacityMin} AND {DomainLimits.CapacityMax}");
            t.HasCheckConstraint("ck_teams_wip_limit_in_progress",
                $"wip_limit_in_progress BETWEEN {DomainLimits.WipLimitMin} AND {DomainLimits.WipLimitMax}");
            t.HasCheckConstraint("ck_teams_wip_limit_review",
                $"wip_limit_review BETWEEN {DomainLimits.WipLimitMin} AND {DomainLimits.WipLimitMax}");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(DomainLimits.TeamNameMaxLength).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
