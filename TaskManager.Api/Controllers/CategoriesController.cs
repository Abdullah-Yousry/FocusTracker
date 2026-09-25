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
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryRequestDto dto)
        {
            var category = await _categoryService.CreateAsync(dto, UserId);
            return CreatedAtAction(nameof(GetAll), new { id = category.CategoryId }, category);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id, UserId);
            return Ok(category);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CategoryRequestDto dto)
        {
            var category = await _categoryService.UpdateAsync(id, UserId, dto);
            return Ok(category);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id, UserId);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParamsDto dto)
        {
            var result = await _categoryService.GetUserCategoriesAsync(UserId, dto);
            return Ok(result);
        }
    }
}
