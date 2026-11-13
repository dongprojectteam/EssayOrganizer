using EssayOrganizer.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(EssayDbContextFactory factory)
    {
        await using var db = factory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Profiles (Id TEXT NOT NULL CONSTRAINT PK_Profiles PRIMARY KEY, Name TEXT NOT NULL, Avatar TEXT NOT NULL, CreatedAt TEXT NOT NULL);");
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Essays ADD COLUMN ProfileId TEXT NULL;");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Existing database already has the column.
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Essays ADD COLUMN FormattedBody TEXT NOT NULL DEFAULT '';");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Existing database already has the column.
        }

        var legacyProfile = await db.Profiles.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (legacyProfile is null)
        {
            legacyProfile = new Profile { Id = Guid.NewGuid(), Name = "내 프로파일", Avatar = "violet", CreatedAt = DateTime.UtcNow };
            db.Profiles.Add(legacyProfile);
            await db.SaveChangesAsync();
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Essays SET ProfileId = {legacyProfile.Id} WHERE ProfileId IS NULL OR ProfileId = '';");
    }
}
