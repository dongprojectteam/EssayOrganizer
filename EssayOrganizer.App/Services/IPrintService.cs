using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public interface IPrintService
{
    void Print(Essay essay, FontSettings settings);
}

