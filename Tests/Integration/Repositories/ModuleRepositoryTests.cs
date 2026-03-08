using AutoMapper;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Repository.Mapping;
using Repository.Repositories;

namespace Tests.Integration.Repositories;

public class ModuleRepositoryTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<EntityToDomainProfile>())
            .CreateMapper();

    private static ModuleEntity MakeModule(Guid pageId, int x, int y, int w, int h, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            LessonPageId = pageId,
            ModuleType = ModuleType.FreeText,
            GridX = x,
            GridY = y,
            GridWidth = w,
            GridHeight = h,
            ContentJson = "[]"
        };

    [Fact]
    public async Task CheckOverlapAsync_EmptyPage_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var repo = new ModuleRepository(ctx, CreateMapper());

        var result = await repo.CheckOverlapAsync(Guid.NewGuid(), 0, 0, 5, 5);

        Assert.False(result);
    }

    [Fact]
    public async Task CheckOverlapAsync_NonOverlappingModules_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var pageId = Guid.NewGuid();

        ctx.Modules.Add(MakeModule(pageId, x: 0, y: 0, w: 5, h: 5));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new ModuleRepository(ctx, CreateMapper());

        // Placed at x=5 — shares the edge but does not overlap (5 > 5 is false)
        var result = await repo.CheckOverlapAsync(pageId, gridX: 5, gridY: 0, gridWidth: 5, gridHeight: 5);

        Assert.False(result);
    }

    [Fact]
    public async Task CheckOverlapAsync_OverlappingModule_ReturnsTrue()
    {
        await using var ctx = CreateContext();
        var pageId = Guid.NewGuid();

        ctx.Modules.Add(MakeModule(pageId, x: 0, y: 0, w: 5, h: 5));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new ModuleRepository(ctx, CreateMapper());

        // Proposed rect (3,3,5,5) overlaps existing (0,0,5,5) on both axes
        var result = await repo.CheckOverlapAsync(pageId, gridX: 3, gridY: 3, gridWidth: 5, gridHeight: 5);

        Assert.True(result);
    }

    [Fact]
    public async Task CheckOverlapAsync_ExcludeModuleId_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var pageId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        ctx.Modules.Add(MakeModule(pageId, x: 0, y: 0, w: 5, h: 5, id: moduleId));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new ModuleRepository(ctx, CreateMapper());

        // Same position — would overlap, but the module is excluded (update scenario)
        var result = await repo.CheckOverlapAsync(
            pageId, gridX: 0, gridY: 0, gridWidth: 5, gridHeight: 5,
            excludeModuleId: moduleId);

        Assert.False(result);
    }
}
