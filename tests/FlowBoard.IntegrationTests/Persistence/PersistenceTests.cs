using FlowBoard.Api.Domain.Entities;
using FlowBoard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowBoard.IntegrationTests.Persistence;

/// <summary>Esquema, constraints do banco e concorrência otimista (xmin) com PostgreSQL real.</summary>
public class PersistenceTests(ApiFixture api)
{
    private static readonly DateTime Now = new(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CheckConstraint_RejectsInvalidStoryPoints_EvenBypassingTheDomain()
    {
        // Arrange: Feature válida
        var (featureId, _) = await CreateFeatureAsync();
        await using var db = api.CreateDbContext();

        // Act: INSERT direto em SQL, sem passar pelas regras da API (defesa em profundidade)
        var ex = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"INSERT INTO stories (feature_id, title, story_points, status, created_at) VALUES ({featureId}, 'x', 4, 'Backlog', now())",
            TestContext.Current.CancellationToken));

        // Assert: 23514 = check_violation
        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_stories_story_points", ex.ConstraintName);
    }

    [Fact]
    public async Task ConcurrentUpdatesOfSameStory_SecondSaveThrowsConcurrencyException()
    {
        var ct = TestContext.Current.CancellationToken;
        var (featureId, pi) = await CreateFeatureAsync();
        int storyId;
        await using (var setup = api.CreateDbContext())
        {
            var story = new Story(featureId, pi, "Concorrência", null, null, null, null, Now);
            setup.Stories.Add(story);
            await setup.SaveChangesAsync(ct);
            storyId = story.Id;
        }

        // Duas "requisições" carregam a mesma versão da Story.
        await using var first = api.CreateDbContext();
        await using var second = api.CreateDbContext();
        var a = await first.Stories.SingleAsync(s => s.Id == storyId, ct);
        var b = await second.Stories.SingleAsync(s => s.Id == storyId, ct);

        a.UpdatePlanning(pi, "Primeira edição", null, 3, null, null);
        await first.SaveChangesAsync(ct);

        b.UpdatePlanning(pi, "Segunda edição", null, 5, null, null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(ct));
    }

    private async Task<(int FeatureId, ProgramIncrement Pi)> CreateFeatureAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = api.CreateDbContext();
        var pi = new ProgramIncrement(ApiFixture.Unique("PI"), new DateOnly(2026, 11, 23), 5, 2, Now);
        db.ProgramIncrements.Add(pi);
        await db.SaveChangesAsync(ct);
        var feature = new Feature(pi.Id, "Feature de teste", null, 8, 5, 3, 5, Now);
        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return (feature.Id, pi);
    }
}
