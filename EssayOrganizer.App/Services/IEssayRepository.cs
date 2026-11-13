using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public interface IEssayRepository
{
    Task<List<Essay>> GetAllAsync(Guid profileId);
    Task<List<Essay>> SearchAsync(Guid profileId, string query);
    Task<Essay?> GetByIdAsync(Guid profileId, Guid id);
    Task AddAsync(Essay essay);
    Task UpdateAsync(Essay essay);
    Task MoveAsync(Guid id, Guid sourceProfileId, Guid targetProfileId);
    Task DeleteAsync(Guid profileId, Guid id);
}

