using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;
using TaskManager.Application.DTOs.TaskManager.Application.DTOs;

namespace TaskManager.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto, int userId);
        Task<CategoryResponseDto> UpdateAsync(int id, int userId, CategoryRequestDto dto);
        Task DeleteAsync(int id, int userId);
        Task<CategoryResponseDto> GetByIdAsync(int id, int userId);
        //Task<IEnumerable<CategoryResponseDto>> GetUserCategoriesAsync(int userId);
        Task<PagedResultDto<CategoryResponseDto>> GetUserCategoriesAsync(int userId, PaginationParamsDto dto);
    }
}
