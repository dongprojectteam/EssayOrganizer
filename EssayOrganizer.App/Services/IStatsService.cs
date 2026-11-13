using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public interface IStatsService
{
    EssayStats Calculate(string bodyText);
}

