using Kartly.Application.DTOs.Products;

namespace Kartly.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? query, string? category);
    Task<ProductDto> GetByIdAsync(Guid id);

    // Admin-only
    Task<ProductDto> CreateAsync(Guid adminUserId, CreateProductDto dto);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}
