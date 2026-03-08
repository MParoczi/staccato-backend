using AutoMapper;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Repository.Mapping;
using Repository.Repositories;

namespace Tests.Integration.Repositories;

/// <summary>
///     Uses SQLite in-memory (not EF InMemory) because RevokeAllForUserAsync uses
///     ExecuteUpdateAsync which requires a relational provider.
/// </summary>
public class RefreshTokenRepositoryTests
{
    private static async Task<(AppDbContext ctx, IMapper mapper, SqliteConnection conn)> CreateSetupAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options);

        // EnsureCreated() fails with SQLite because EF Core configurations use
        // HasColumnType("nvarchar(max)") which is SQL Server-only syntax.
        // We manually create only the two tables required by these tests.
        await ctx.Database.ExecuteSqlRawAsync("""
                                              CREATE TABLE "Users" (
                                                  "Id"                  TEXT NOT NULL PRIMARY KEY,
                                                  "Email"               TEXT NOT NULL,
                                                  "PasswordHash"        TEXT,
                                                  "GoogleId"            TEXT,
                                                  "FirstName"           TEXT NOT NULL,
                                                  "LastName"            TEXT NOT NULL,
                                                  "AvatarUrl"           TEXT,
                                                  "CreatedAt"           TEXT NOT NULL,
                                                  "ScheduledDeletionAt" TEXT,
                                                  "Language"            INTEGER NOT NULL
                                              );

                                              CREATE TABLE "RefreshTokens" (
                                                  "Id"        TEXT    NOT NULL PRIMARY KEY,
                                                  "Token"     TEXT    NOT NULL,
                                                  "UserId"    TEXT    NOT NULL REFERENCES "Users" ("Id") ON DELETE CASCADE,
                                                  "ExpiresAt" TEXT    NOT NULL,
                                                  "CreatedAt" TEXT    NOT NULL,
                                                  "IsRevoked" INTEGER NOT NULL
                                              );
                                              """);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<EntityToDomainProfile>())
            .CreateMapper();

        return (ctx, mapper, conn);
    }

    private static UserEntity MakeUser(Guid userId)
    {
        return new UserEntity
        {
            Id = userId,
            Email = $"{userId}@test.com",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            Language = Language.English
        };
    }

    private static RefreshTokenEntity MakeToken(Guid userId, bool isRevoked)
    {
        return new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = isRevoked
        };
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesOnlyActiveTokens()
    {
        var (ctx, mapper, conn) = await CreateSetupAsync();
        await using (ctx)
        await using (conn)
        {
            var userId = Guid.NewGuid();
            ctx.Users.Add(MakeUser(userId));
            ctx.RefreshTokens.AddRange(
                MakeToken(userId, false), // active → should be revoked
                MakeToken(userId, false), // active → should be revoked
                MakeToken(userId, true) // already revoked → unchanged
            );
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var repo = new RefreshTokenRepository(ctx, mapper);
            await repo.RevokeAllForUserAsync(userId);

            ctx.ChangeTracker.Clear();
            var allTokens = await ctx.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            Assert.Equal(3, allTokens.Count);
            Assert.All(allTokens, t => Assert.True(t.IsRevoked));
        }
    }

    [Fact]
    public async Task RevokeAllForUserAsync_CommitsImmediately_WithoutExplicitCommit()
    {
        var (ctx, mapper, conn) = await CreateSetupAsync();
        await using (ctx)
        await using (conn)
        {
            var userId = Guid.NewGuid();
            ctx.Users.Add(MakeUser(userId));
            ctx.RefreshTokens.Add(MakeToken(userId, false));
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var repo = new RefreshTokenRepository(ctx, mapper);

            // Call without following CommitAsync — ExecuteUpdateAsync commits directly
            await repo.RevokeAllForUserAsync(userId);

            // Clear the change tracker and re-query to prove the DB was updated
            ctx.ChangeTracker.Clear();
            var token = await ctx.RefreshTokens.SingleAsync(t => t.UserId == userId);

            Assert.True(token.IsRevoked);
        }
    }
}