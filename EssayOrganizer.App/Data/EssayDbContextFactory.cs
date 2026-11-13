using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Data;

public class EssayDbContextFactory
{
    private readonly string _databasePath;

    public EssayDbContextFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public EssayDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EssayDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new EssayDbContext(options);
    }
}

