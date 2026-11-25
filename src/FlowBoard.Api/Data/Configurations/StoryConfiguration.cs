using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Api.Data.Configurations;

/// <summary>Mapeamento da tabela "stories" (status como texto, índices para WIP e capacidade).</summary>
public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.ToTable("stories", t =>
        {
            t.HasCheckConstraint("ck_stories_story_points", "story_points IS NULL OR story_points IN (1, 2, 3, 5, 8, 13)");
            t.HasCheckConstraint("ck_stories_iteration", "iteration IS NULL OR iteration >= 1");
            t.HasCheckConstraint("ck_stories_status",
                "status IN ('Backlog', 'Ready', 'InProgress', 'Review', 'Done')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(DomainLimits.TitleMaxLength).IsRequired();
        builder.Property(s => s.AcceptanceCriteria).HasMaxLength(DomainLimits.AcceptanceCriteriaMaxLength);

        // Enum salvo como texto ("InProgress"), legível no banco.
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // xmin: coluna de sistema do PostgreSQL usada como token de concorrência otimista.
        builder.Property(s => s.Version).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        builder.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(s => s.FeatureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TeamId, s.Status });    // contagem de WIP (R2) e quadro
        builder.HasIndex(s => new { s.TeamId, s.Iteration }); // soma de capacidade (R5)
    }
}
