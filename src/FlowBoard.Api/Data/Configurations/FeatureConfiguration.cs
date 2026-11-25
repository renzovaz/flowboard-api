using FlowBoard.Api.Domain.Entities;
using FlowBoard.Api.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Api.Data.Configurations;

/// <summary>Mapeamento da tabela "features" (WSJF não é armazenado).</summary>
public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    private const string WsjfScale = "(1, 2, 3, 5, 8, 13, 20)";

    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("features", t =>
        {
            t.HasCheckConstraint("ck_features_business_value", $"business_value IN {WsjfScale}");
            t.HasCheckConstraint("ck_features_time_criticality", $"time_criticality IN {WsjfScale}");
            t.HasCheckConstraint("ck_features_risk_reduction", $"risk_reduction IN {WsjfScale}");
            t.HasCheckConstraint("ck_features_job_size", $"job_size IN {WsjfScale}");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Title).HasMaxLength(DomainLimits.TitleMaxLength).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(DomainLimits.DescriptionMaxLength);

        builder.Ignore(f => f.CostOfDelay);
        builder.Ignore(f => f.Wsjf);

        // Um PI tem várias Features. Restrict: nunca apagar em cascata.
        builder.HasOne<ProgramIncrement>()
            .WithMany()
            .HasForeignKey(f => f.ProgramIncrementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
