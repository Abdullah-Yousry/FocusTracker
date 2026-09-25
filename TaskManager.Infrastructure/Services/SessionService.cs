using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionService> _logger;

        public SessionService(AppDbContext context, ILogger<SessionService> logger) 
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SessionResponseDto> CreateAsync(CreateSessionDto dto, int userId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.UserId == userId);
            if(category == null)
            {
                throw new KeyNotFoundException($"Category with id {dto.CategoryId} was not found.");
            }

            var session = new Session()
            {
                UserId = userId,
                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Status = Status.Pending,
                Category = category,
                MaxAllowedPauseInMinutes = dto.MaxAllowedPauseMinutes,
            };
            await _context.Sessions.AddAsync(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session with id {SessionId} created for User {UserId}", session.SessionId, userId);

            return MapToDto(session);
        }

        public async Task<SessionResponseDto> RunAsync(int sessionId, int userId)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);

            if (session.Status == Status.Finished)
                throw new InvalidOperationException("Cannot run a finished session.");

            if (session.Status == Status.Running)
                throw new InvalidOperationException("Session is already running.");

            if (session.Status == Status.Pending)
                session.StartedAt = DateTime.UtcNow;

            if (session.Status == Status.Paused)
                session.TotalPausedInMinutes += (int)(DateTime.UtcNow - session.LastStatusChangedAt).TotalMinutes;

            session.Status = Status.Running;
            session.LastStatusChangedAt = DateTime.UtcNow;  
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session with id {SessionId} for User {UserId} is running", session.SessionId, userId);

            return MapToDto(session);
        }

        public async Task<SessionResponseDto> PauseAsync(int sessionId, int userId)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);

            if (session.Status != Status.Running)
                throw new InvalidOperationException("Only running sessions can be paused.");

            if(session.TotalPausedInMinutes >= session.MaxAllowedPauseInMinutes)
                throw new InvalidOperationException("Reached to the allowed pause time. Please finish the session or continue.");

            var minutes = (int)(DateTime.UtcNow - session.LastStatusChangedAt).TotalMinutes;
            session.FocusTimeInMinutes += minutes;

            session.Status = Status.Paused;
            session.LastStatusChangedAt = DateTime.UtcNow;
            session.PauseCount++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session with id {SessionId} for User {UserId} is paused", session.SessionId, userId);

            return MapToDto(session);
        }

        public async Task<SessionResponseDto> FinishAsync(int sessionId, int userId, FinishSessionDto? dto = null)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);



            if (session.Status == Status.Finished)
                throw new InvalidOperationException("Session is already finished.");

            if (session.Status == Status.Running)
            {
                var minutes = (int)(DateTime.UtcNow - session.LastStatusChangedAt).TotalMinutes;
                session.FocusTimeInMinutes += minutes;
            } 
            session.Status = Status.Finished;
            session.LastStatusChangedAt = DateTime.UtcNow;
            if(dto != null && !string.IsNullOrWhiteSpace(dto.Summary))
                session.Summary = dto.Summary;
            session.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session with id {SessionId} for User {UserId} is completed", session.SessionId, userId);

            return MapToDto(session);
        }

        public async Task<SessionResponseDto> GetByIdAsync(int sessionId, int userId)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);
            return MapToDto(session);
        }

        public async Task DeleteAsync(int sessionId, int userId)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} deleted by User {UserId}", sessionId, userId);
        }

        public async Task<SessionResponseDto> UpdateAsync(int sessionId, int userId, UpdateSessionDto dto)
        {
            var session = await GetSessionOrThrowAsync(sessionId, userId);

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.UserId == userId);
            if (category == null)
                throw new KeyNotFoundException($"Category with id {dto.CategoryId} was not found.");

            session.CategoryId = dto.CategoryId;
            session.Title = dto.Title;
            session.Summary = dto.Summary;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session {SessionId} updated by User {UserId}", session.SessionId, userId);

            return MapToDto(session);
        }

        public async Task<DaySummaryDto> GetDaySummaryAsync(int userId, DateTime date)
        {
            var targetDate = date.Date;
            var nextTargetDate = targetDate.AddDays(1);

            var requiredSessionsIds = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.StartedAt.HasValue && s.StartedAt.Value >= targetDate && s.StartedAt.Value < nextTargetDate)
                .Select(s => s.SessionId)
                .ToListAsync();

            await BulkUpdate(requiredSessionsIds);

            var sessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Category)
                .Where(s => requiredSessionsIds.Contains(s.SessionId))
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            var sessionDtos = sessions.Select(MapToDto).ToList();
            var totalMinutes = sessionDtos.Sum(s => s.FocusTimeInMinutes);

            return new DaySummaryDto
            {
                Date = targetDate,
                TotalMinutes = totalMinutes,
                Sessions = sessionDtos
            };
        }

        public async Task<PagedResultDto<SessionResponseDto>> GetUserSessionsAsync(int userId, PaginationParamsDto dto)
        {
            var baseQuery = _context.Sessions.Where(s => s.UserId == userId);

            int count = await baseQuery.CountAsync();

            var requiredSessionsIds = await baseQuery
                .OrderByDescending(s => s.CreatedAt)
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(s => s.SessionId)
                .ToListAsync();

            await BulkUpdate(requiredSessionsIds);

                var sessions = await _context.Sessions
                    .AsNoTracking()
                    .Include(s => s.Category)
                    .Where(s => requiredSessionsIds.Contains(s.SessionId))
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            
            var sessionDtos = sessions.Select(MapToDto).ToList();

            return new PagedResultDto<SessionResponseDto>
            {
                Items = sessionDtos,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                TotalCount = count
            };
        }

        private static SessionResponseDto MapToDto(Session session)
        {
            var currentTotalTime = session.FocusTimeInMinutes;

            if (session.Status == Status.Running)
            {
                var minutes = (int)(DateTime.UtcNow - session.LastStatusChangedAt).TotalMinutes;
                currentTotalTime += minutes;
            }

            return new SessionResponseDto
            {
                SessionId = session.SessionId,
                Title = session.Title,
                Summary = session.Summary,
                CategoryId = session.CategoryId,
                Status = session.Status,
                FocusTimeInMinutes = currentTotalTime,
                CreateAt = session.CreatedAt,
                LastStatusChangedAt = session.LastStatusChangedAt,
                PausedCount = session.PauseCount,
                CategoryName = session.Category?.Name ?? string.Empty,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                MaxAllowedPauseInMinutes = session.MaxAllowedPauseInMinutes,
                TotalPausedInMinutes = session.TotalPausedInMinutes,
            };
        }

        private async Task<Session> GetSessionOrThrowAsync(int sessionId, int userId)
        { 
            var session = await _context.Sessions
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);
            if (session == null)
                throw new KeyNotFoundException($"Session with id {sessionId} was not found.");


            await AutoFinishedForPauseSession(session);

            return session;
        }

        private async Task AutoFinishedForPauseSession(Session session)
        {
            if (session != null && session.Status == Status.Paused)
            {
                var evaluate = EvaluatePauseStatus(session);
                if(evaluate.Status == Status.Finished)
                {
                    session.Status = evaluate.Status;
                    session.TotalPausedInMinutes = evaluate.TotalPausedInMinutes;
                    session.LastStatusChangedAt = evaluate.LastStatusChangedAt;
                    session.EndedAt = evaluate.EndedAt;
                    session.Summary = evaluate.summary;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Session with id {SessionId} for User {UserId} is Auto-finished due to extended pause duration", session.SessionId, session.UserId);
                }
            }
        }
        private static (Status Status, int TotalPausedInMinutes, DateTime LastStatusChangedAt, DateTime? EndedAt, string? summary) EvaluatePauseStatus(Session session)
        {
            var status = session.Status;
            var totalPausedInMinutes = session.TotalPausedInMinutes;
            var lastStatusChangedAt = session.LastStatusChangedAt;
            var endedAt = session.EndedAt;
            var summary = session.Summary;
            

            if (session.Status == Status.Paused)
            {
                var pauseTime = (int)(DateTime.UtcNow - session.LastStatusChangedAt).TotalMinutes;

                if (pauseTime + session.TotalPausedInMinutes > session.MaxAllowedPauseInMinutes)
                {
                    var remainAlowTime = session.MaxAllowedPauseInMinutes - session.TotalPausedInMinutes;
                    var autoFinishTime = session.LastStatusChangedAt.AddMinutes(remainAlowTime);

                    status = Status.Finished;
                    lastStatusChangedAt = autoFinishTime;
                    endedAt = autoFinishTime;
                    totalPausedInMinutes = session.MaxAllowedPauseInMinutes;
                    summary = string.IsNullOrWhiteSpace(session.Summary)
                        ? "Auto-finished due to extended pause duration."
                        : session.Summary + " (Auto-finished due to extended pause duration).";
                }
            }

            return (status, totalPausedInMinutes, lastStatusChangedAt, endedAt, summary);
        }
        private async Task BulkUpdate(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return;

            var now = DateTime.UtcNow;

            int autoFinishedCount = await _context.Sessions
                .Where(s =>
                    s.Status == Status.Paused
                    && ids.Contains(s.SessionId)
                    && EF.Functions.DateDiffMinute(s.LastStatusChangedAt, now) + s.TotalPausedInMinutes > s.MaxAllowedPauseInMinutes)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, Status.Finished)
                    .SetProperty(x => x.TotalPausedInMinutes, x => x.MaxAllowedPauseInMinutes)
                    .SetProperty(x => x.LastStatusChangedAt, x => x.LastStatusChangedAt.AddMinutes(x.MaxAllowedPauseInMinutes - x.TotalPausedInMinutes))
                    .SetProperty(x => x.EndedAt, x => x.LastStatusChangedAt.AddMinutes(x.MaxAllowedPauseInMinutes - x.TotalPausedInMinutes))
                    .SetProperty(x => x.Summary, x => (x.Summary == null || x.Summary == "")
                        ? "Auto-finished due to extended pause duration."
                        : x.Summary + " (Auto-finished due to extended pause duration).")
                );

            if(autoFinishedCount > 0)
                _logger.LogInformation("BulkUpdate: Auto-finished {Count} sessions due to extended pause duration.", autoFinishedCount);
        }
    }
}
