using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryViewModel>> GetAllCategoriesAsync()
        {
            var today = DateTime.Today;
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder,
                    MedicineCount = c.Medicines.Count(m => m.IsActive && m.ExpiryDate > today)
                })
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<ApiResponse<Category>> CreateAsync(CategoryCreateViewModel model)
        {
            try
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive,
                    DisplayOrder = model.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();

                return new ApiResponse<Category>
                {
                    Success = true,
                    Message = "Category created successfully",
                    Data = category
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Category>
                {
                    Success = false,
                    Message = $"Error creating category: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Category>> UpdateAsync(int id, CategoryCreateViewModel model)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return new ApiResponse<Category> { Success = false, Message = "Category not found" };
                }

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageUrl = model.ImageUrl;
                category.IsActive = model.IsActive;
                category.DisplayOrder = model.DisplayOrder;

                await _context.SaveChangesAsync();

                return new ApiResponse<Category>
                {
                    Success = true,
                    Message = "Category updated successfully",
                    Data = category
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Category>
                {
                    Success = false,
                    Message = $"Error updating category: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Medicines)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Category not found" };
                }

                if (category.Medicines.Any(m => m.IsActive))
                {
                    return new ApiResponse<bool> { Success = false, Message = "Cannot delete category with active medicines" };
                }

                category.IsActive = false;
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Category deleted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error deleting category: {ex.Message}"
                };
            }
        }
    }
}
