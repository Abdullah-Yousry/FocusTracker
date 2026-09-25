using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;
using static System.Collections.Specialized.BitVector32;

namespace TaskManager.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(AppDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto, int userId)
        {
            var cleanName = Regex.Replace(dto.Name.Trim(), @"\s+", " ").ToLower();

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.UserId == userId && c.Name == cleanName);
            if (categoryExists)
                throw new ArgumentException($"A category with the name '{dto.Name}' already exists.");

            var category = new Category
            {
                Name = cleanName,
                UserId = userId,
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category with id {CategoryId} created for User {UserId}", category.CategoryId, userId);

            return new CategoryResponseDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
            };
        }

        public async Task<CategoryResponseDto> GetByIdAsync(int id, int userId)
        {
            var category = await GetCategoryOrThrowAsync(id, userId);
            return MapToDto(category);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var category = await GetCategoryOrThrowAsync(id, userId);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category {CategoryId} deleted by User {UserId}", id, userId);
        }

        public async Task<CategoryResponseDto> UpdateAsync(int id, int userId, CategoryRequestDto dto)
        {
            var category = await GetCategoryOrThrowAsync(id, userId);

            category.Name = dto.Name;

            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        public async Task<PagedResultDto<CategoryResponseDto>> GetUserCategoriesAsync(int userId, PaginationParamsDto dto)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId);

            int count = await query.CountAsync();

            var categories = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var categoryDtos = categories.Select(MapToDto).ToList();

            return new PagedResultDto<CategoryResponseDto>
            {
                Items = categoryDtos,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                TotalCount = count
            };
        }

        private static CategoryResponseDto MapToDto(Category category) => new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name
        };
        private async Task<Category> GetCategoryOrThrowAsync(int categoryId, int userId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.UserId == userId);
            if(category == null)
                throw new KeyNotFoundException($"Category with id {categoryId} was not found.");
            
            return category;
        }

    }
}
