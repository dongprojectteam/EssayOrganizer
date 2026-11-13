using EssayOrganizer.App.Data;
using EssayOrganizer.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Services;

public class EssayRepository : IEssayRepository
{
    private readonly EssayDbContextFactory _factory;

    public EssayRepository(EssayDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Essay>> GetAllAsync(Guid profileId)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Essays
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Essay>> SearchAsync(Guid profileId, string query)
    {
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return await GetAllAsync(profileId);
        }

        await using var db = _factory.CreateDbContext();

        var titleMatches = db.Essays
            .Where(x => x.ProfileId == profileId && EF.Functions.Like(x.Title, $"%{normalized}%"));

        var bodyMatches = db.Essays
            .Where(x => x.ProfileId == profileId && !EF.Functions.Like(x.Title, $"%{normalized}%") && EF.Functions.Like(x.Body, $"%{normalized}%"));

        var ordered = await titleMatches
            .Concat(bodyMatches)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return ordered;
    }

    public async Task<Essay?> GetByIdAsync(Guid profileId, Guid id)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Essays.FirstOrDefaultAsync(x => x.Id == id && x.ProfileId == profileId);
    }

    public async Task AddAsync(Essay essay)
    {
        await using var db = _factory.CreateDbContext();
        db.Essays.Add(essay);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Essay essay)
    {
        await using var db = _factory.CreateDbContext();
        db.Essays.Update(essay);
        await db.SaveChangesAsync();
    }

    public async Task MoveAsync(Guid id, Guid sourceProfileId, Guid targetProfileId)
    {
        await using var db = _factory.CreateDbContext();
        var essay = await db.Essays.FirstOrDefaultAsync(x => x.Id == id && x.ProfileId == sourceProfileId);
        if (essay is null) return;

        essay.ProfileId = targetProfileId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid profileId, Guid id)
    {
        await using var db = _factory.CreateDbContext();
        var essay = await db.Essays.FirstOrDefaultAsync(x => x.Id == id && x.ProfileId == profileId);
        if (essay is null)
        {
            return;
        }

        db.Essays.Remove(essay);
        await db.SaveChangesAsync();
    }
}

