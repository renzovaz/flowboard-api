using System.Text;
using FlowBoard.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Api.Data;

/// <summary>
/// Ponto de acesso ao PostgreSQL via Entity Framework Core.
/// Cada DbSet representa uma tabela; SaveChangesAsync grava tudo em uma única transação
/// (o DbContext já cumpre o papel de "repositório" e "unidade de trabalho").
/// </summary>
public class FlowBoardDbContext(DbContextOptions<FlowBoardDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    public DbSet<ProgramIncrement> ProgramIncrements => Set<ProgramIncrement>();

    public DbSet<Feature> Features => Set<Feature>();

    public DbSet<Story> Stories => Set<Story>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as classes de Configurations/ (uma por entidade).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowBoardDbContext).Assembly);

        // Convenção do PostgreSQL: nomes em snake_case (ex.: CapacityPoints → capacity_points).
        ApplySnakeCaseNames(modelBuilder);
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                // "xmin" é coluna de sistema do PostgreSQL e já tem o nome correto.
                if (property.GetColumnName() != "xmin")
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && name[i - 1] != '_' && !char.IsUpper(name[i - 1]))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
