using EssayOrganizer.App.Data;
using EssayOrganizer.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Services;

public sealed class ProfileRepository(EssayDbContextFactory factory) : IProfileRepository
{
    public async Task<List<Profile>> GetAllAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Profiles.OrderBy(x => x.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(Profile profile)
    {
        await using var db = factory.CreateDbContext();
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Profile profile)
    {
        await using var db = factory.CreateDbContext();
        var stored = await db.Profiles.FirstOrDefaultAsync(x => x.Id == profile.Id);
        if (stored is null) return;
        stored.Name = profile.Name;
        stored.Avatar = profile.Avatar;
        await db.SaveChangesAsync();
    }
}
