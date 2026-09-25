using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.TaskManager.Application.DTOs;

namespace TaskManager.Application.Interfaces
{
    public interface ISessionService
    {
        Task<SessionResponseDto> CreateAsync(CreateSessionDto dto, int userId);
        Task<SessionResponseDto> RunAsync(int sessionId, int userId);
        Task<SessionResponseDto> PauseAsync(int sessionId, int userId);
        Task<SessionResponseDto> FinishAsync(int sessionId, int userId, FinishSessionDto? dto);
        Task<SessionResponseDto> GetByIdAsync(int sessionId, int userId);
        Task DeleteAsync(int sessionId, int userId);
        Task<SessionResponseDto> UpdateAsync(int sessionId, int userId, UpdateSessionDto dto);
        Task<PagedResultDto<SessionResponseDto>> GetUserSessionsAsync(int userId, PaginationParamsDto dto);
        Task<DaySummaryDto> GetDaySummaryAsync(int userId, DateTime date);
    }
}
