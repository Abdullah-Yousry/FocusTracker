using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("StandardPolicy")]
    public class SessionsController : BaseApiController
    {
        private readonly ISessionService _sessionService;
        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateSessionDto dto)
        {
            var session = await _sessionService.CreateAsync(dto, UserId);
            return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, session);
        }

        [HttpPatch("{id:int}/run")]
        public async Task<IActionResult> Run(int id)
        {
            var session = await _sessionService.RunAsync(id, UserId);
            return Ok(session);
        }

        [HttpPatch("{id:int}/pause")]
        public async Task<IActionResult> Pause(int id)
        {
            var session = await _sessionService.PauseAsync(id, UserId);
            return Ok(session);
        }

        [HttpPatch("{id:int}/finish")]
        public async Task<IActionResult> Finish(int id, FinishSessionDto? dto = null)
        {
            var session = await _sessionService.FinishAsync(id, UserId, dto);
            return Ok(session);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var session = await _sessionService.GetByIdAsync(id, UserId);
            return Ok(session);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParamsDto dto)
        {
            var result = await _sessionService.GetUserSessionsAsync(UserId, dto);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateSessionDto dto)
        {
            var session = await _sessionService.UpdateAsync(id,UserId, dto);
            return Ok(session);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _sessionService.DeleteAsync(id, UserId);
            return NoContent();
        }

        [HttpGet("day-summary")]
        public async Task<IActionResult> GetDaySummary([FromQuery] GetDaySummaryQueryDto dto)
        {
            var targetDate = dto.Date;

            var summary = await _sessionService.GetDaySummaryAsync(UserId, targetDate);
            return Ok(summary);
        }
    }
}