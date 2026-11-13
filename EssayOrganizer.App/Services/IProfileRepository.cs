using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public interface IProfileRepository
{
    Task<List<Profile>> GetAllAsync();
    Task AddAsync(Profile profile);
    Task UpdateAsync(Profile profile);
}
